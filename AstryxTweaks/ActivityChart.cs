using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AstryxTweaks;

public class ActivityChart : Panel
{
	private struct FILETIME
	{
		public uint dwLowDateTime;

		public uint dwHighDateTime;
	}

	private struct MEMORYSTATUSEX
	{
		public uint dwLength;

		public uint dwMemoryLoad;

		public ulong ullTotalPhys;

		public ulong ullAvailPhys;

		public ulong ullTotalPageFile;

		public ulong ullAvailPageFile;

		public ulong ullTotalVirtual;

		public ulong ullAvailVirtual;

		public ulong ullAvailExtendedVirtual;
	}

	private const int HistoryLength = 60;

	private readonly Dictionary<string, Queue<float>> history = new Dictionary<string, Queue<float>>
	{
		{
			"CPU",
			new Queue<float>()
		},
		{
			"GPU",
			new Queue<float>()
		},
		{
			"RAM",
			new Queue<float>()
		}
	};

	private readonly Dictionary<string, Rectangle> modeBounds = new Dictionary<string, Rectangle>();

	private readonly Timer sampleTimer;

	private readonly Dictionary<string, PerformanceCounter> gpuCounters = new Dictionary<string, PerformanceCounter>();

	private PerformanceCounter cpuCounter;

	private string mode = "CPU";

	private float cpuUsage;

	private float gpuUsage;

	private float ramUsage;

	private bool gpuCountersInitialized;

	private int gpuRefreshCountdown;

	private ulong previousIdleTime;

	private ulong previousKernelTime;

	private ulong previousUserTime;

	private bool cpuTimesInitialized;

	private int hoverIndex = -1;

	public ActivityChart()
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Expected O, but got Unknown
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Expected O, but got Unknown
		SetStyle((ControlStyles)139282, true);
		DoubleBuffered = true;
		((Control)this).BackColor = Color.FromArgb(13, 17, 29);
		((Control)this).Font = new Font("Segoe UI", 8f);
		((Control)this).Cursor = Cursors.Hand;
		for (int i = 0; i < 60; i++)
		{
			history["CPU"].Enqueue(0f);
			history["GPU"].Enqueue(0f);
			history["RAM"].Enqueue(0f);
		}
		try
		{
			cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
			cpuCounter.NextValue();
		}
		catch
		{
			cpuCounter = null;
		}
		sampleTimer = new Timer();
		sampleTimer.Interval = 1000;
		sampleTimer.Tick += delegate
		{
			SampleHardware();
		};
		sampleTimer.Start();
		SampleHardware();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		int num = -1;
		int num2 = 0;
		foreach (KeyValuePair<string, Rectangle> modeBound in modeBounds)
		{
			if (modeBound.Value.Contains(e.Location))
			{
				num = num2;
				break;
			}
			num2++;
		}
		if (num != hoverIndex)
		{
			hoverIndex = num;
			((Control)this).Invalidate();
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		hoverIndex = -1;
		((Control)this).Invalidate();
	}

	protected override void OnMouseClick(MouseEventArgs e)
	{
		base.OnMouseClick(e);
		foreach (KeyValuePair<string, Rectangle> modeBound in modeBounds)
		{
			if (modeBound.Value.Contains(e.Location))
			{
				mode = modeBound.Key;
				((Control)this).Invalidate();
				break;
			}
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Expected O, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected O, but got Unknown
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Expected O, but got Unknown
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected O, but got Unknown
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Expected O, but got Unknown
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Expected O, but got Unknown
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Expected O, but got Unknown
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Expected O, but got Unknown
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d7: Expected O, but got Unknown
		//IL_0458: Unknown result type (might be due to invalid IL or missing references)
		//IL_045f: Expected O, but got Unknown
		//IL_0462: Unknown result type (might be due to invalid IL or missing references)
		//IL_046e: Expected O, but got Unknown
		//IL_049a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a1: Expected O, but got Unknown
		//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04af: Expected O, but got Unknown
		//IL_04b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bb: Expected O, but got Unknown
		//IL_04f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_051f: Expected O, but got Unknown
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = (SmoothingMode)4;
		graphics.TextRenderingHint = (TextRenderingHint)5;
		Rectangle rectangle = new Rectangle(0, 0, Math.Max(1, ((Control)this).Width - 1), Math.Max(1, ((Control)this).Height - 1));
		GraphicsPath val = Rounded(rectangle, 16);
		try
		{
			LinearGradientBrush val2 = new LinearGradientBrush(rectangle, Color.FromArgb(19, 27, 48), Color.FromArgb(10, 14, 25), (LinearGradientMode)1);
			try
			{
				Pen val3 = new Pen(Color.FromArgb(42, 91, 186), 1f);
				try
				{
					graphics.FillPath((Brush)val2, val);
					graphics.DrawPath(val3, val);
				}
				finally
				{
					((IDisposable)val3)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		Font val4 = new Font("Segoe UI Semibold", 11f);
		try
		{
			Font val5 = new Font("Segoe UI Semibold", 10f);
			try
			{
				SolidBrush val6 = new SolidBrush(Color.White);
				try
				{
					SolidBrush val7 = new SolidBrush(Color.FromArgb(142, 163, 202));
					try
					{
						graphics.DrawString("LIVE SYSTEM ACTIVITY", val4, (Brush)val6, 18f, 14f);
						graphics.DrawString("Updates every second", ((Control)this).Font, (Brush)val7, 18f, 37f);
						DrawMetric(graphics, "CPU", cpuUsage, 0, val5);
						DrawMetric(graphics, "GPU", gpuUsage, 1, val5);
						DrawMetric(graphics, "RAM", ramUsage, 2, val5);
					}
					finally
					{
						((IDisposable)val7)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val6)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val5)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val4)?.Dispose();
		}
		int num = 54;
		int num2 = Math.Max(num + 20, ((Control)this).Width - 18);
		int num3 = 78;
		int num4 = Math.Max(num3 + 20, ((Control)this).Height - 29);
		Pen val8 = new Pen(Color.FromArgb(31, 45, 73), 1f);
		try
		{
			SolidBrush val9 = new SolidBrush(Color.FromArgb(105, 128, 171));
			try
			{
				for (int i = 0; i < 5; i++)
				{
					int num5 = num3 + (num4 - num3) * i / 4;
					graphics.DrawLine(val8, num, num5, num2, num5);
					graphics.DrawString(100 - i * 25 + "%", ((Control)this).Font, (Brush)val9, 15f, (float)num5 - 7f);
				}
				string[] array = new string[5] { "60s", "45s", "30s", "15s", "now" };
				for (int j = 0; j < array.Length; j++)
				{
					float num6 = (float)num + (float)((num2 - num) * j) / 4f;
					graphics.DrawString(array[j], ((Control)this).Font, (Brush)val9, num6 - 9f, (float)num4 + 7f);
				}
			}
			finally
			{
				((IDisposable)val9)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val8)?.Dispose();
		}
		float[] array2 = history[mode].ToArray();
		if (array2.Length < 2)
		{
			return;
		}
		PointF[] array3 = new PointF[array2.Length];
		for (int k = 0; k < array2.Length; k++)
		{
			float num7 = (float)k / (float)(array2.Length - 1);
			float num8 = Math.Max(0f, Math.Min(100f, array2[k]));
			array3[k] = new PointF((float)num + (float)(num2 - num) * num7, (float)num4 - (float)(num4 - num3) * num8 / 100f);
		}
		Color modeColor = GetModeColor(mode);
		GraphicsPath val10 = new GraphicsPath();
		try
		{
			val10.AddLines(array3);
			val10.AddLine(array3[array3.Length - 1].X, array3[array3.Length - 1].Y, (float)num2, (float)num4);
			val10.AddLine(num2, num4, num, num4);
			val10.CloseFigure();
			LinearGradientBrush val11 = new LinearGradientBrush(new Rectangle(num, num3, Math.Max(1, num2 - num), Math.Max(1, num4 - num3)), Color.FromArgb(100, modeColor), Color.FromArgb(3, modeColor), (LinearGradientMode)1);
			try
			{
				graphics.FillPath((Brush)val11, val10);
			}
			finally
			{
				((IDisposable)val11)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val10)?.Dispose();
		}
		Pen val12 = new Pen(Color.FromArgb(75, modeColor), 7f);
		try
		{
			Pen val13 = new Pen(modeColor, 2.6f);
			try
			{
				SolidBrush val14 = new SolidBrush(Color.White);
				try
				{
					val12.LineJoin = (LineJoin)2;
					val13.LineJoin = (LineJoin)2;
					graphics.DrawLines(val12, array3);
					graphics.DrawLines(val13, array3);
					PointF pointF = array3[array3.Length - 1];
					graphics.FillEllipse((Brush)val14, pointF.X - 4f, pointF.Y - 4f, 8f, 8f);
				}
				finally
				{
					((IDisposable)val14)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val13)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val12)?.Dispose();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			sampleTimer.Stop();
			((Component)(object)sampleTimer).Dispose();
			if (cpuCounter != null)
			{
				((Component)(object)cpuCounter).Dispose();
			}
			foreach (Component value in gpuCounters.Values)
			{
				value.Dispose();
			}
		}
		base.Dispose(disposing);
	}

	private void DrawMetric(Graphics graphics, string name, float value, int index, Font valueFont)
	{
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Expected O, but got Unknown
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Expected O, but got Unknown
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Expected O, but got Unknown
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Expected O, but got Unknown
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected O, but got Unknown
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Expected O, but got Unknown
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Expected O, but got Unknown
		int num = 90;
		int num2 = 6;
		int x = ((Control)this).Width - 18 - (3 - index) * num - (2 - index) * num2;
		Rectangle rectangle = new Rectangle(x, 12, num, 45);
		modeBounds[name] = rectangle;
		bool flag = mode == name;
		bool num3 = hoverIndex == index;
		Color modeColor = GetModeColor(name);
		Color color = (flag ? Color.FromArgb(75, 118, 255) : Color.FromArgb(28, 43, 72));
		Color color2 = (flag ? Color.FromArgb(31, 84, 223) : Color.FromArgb(20, 31, 53));
		if (num3 && !flag)
		{
			color = Color.FromArgb(38, 62, 105);
			color2 = Color.FromArgb(27, 43, 76);
		}
		GraphicsPath val = Rounded(rectangle, 11);
		try
		{
			LinearGradientBrush val2 = new LinearGradientBrush(rectangle, color, color2, (LinearGradientMode)2);
			try
			{
				Pen val3 = new Pen(flag ? Color.FromArgb(152, 187, 255) : Color.FromArgb(47, 72, 116));
				try
				{
					SolidBrush val4 = new SolidBrush(flag ? Color.White : Color.FromArgb(150, 172, 209));
					try
					{
						SolidBrush val5 = new SolidBrush(flag ? Color.White : modeColor);
						try
						{
							graphics.FillPath((Brush)val2, val);
							graphics.DrawPath(val3, val);
							graphics.DrawString(name, ((Control)this).Font, (Brush)val4, (float)rectangle.X + 10f, (float)rectangle.Y + 7f);
							graphics.DrawString(Math.Round(value) + "%", valueFont, (Brush)val5, (float)rectangle.X + 48f, (float)rectangle.Y + 13f);
						}
						finally
						{
							((IDisposable)val5)?.Dispose();
						}
					}
					finally
					{
						((IDisposable)val4)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val3)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private void SampleHardware()
	{
		cpuUsage = ReadCpu();
		gpuUsage = ReadGpu();
		ramUsage = ReadRam();
		Push("CPU", cpuUsage);
		Push("GPU", gpuUsage);
		Push("RAM", ramUsage);
		((Control)this).Invalidate();
	}

	private float ReadCpu()
	{
		try
		{
			if (GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
			{
				ulong num = ToUInt64(idleTime);
				ulong num2 = ToUInt64(kernelTime);
				ulong num3 = ToUInt64(userTime);
				if (!cpuTimesInitialized)
				{
					cpuTimesInitialized = true;
					previousIdleTime = num;
					previousKernelTime = num2;
					previousUserTime = num3;
					return cpuUsage;
				}
				ulong num4 = num - previousIdleTime;
				ulong num5 = num2 - previousKernelTime;
				ulong num6 = num3 - previousUserTime;
				ulong num7 = num5 + num6;
				previousIdleTime = num;
				previousKernelTime = num2;
				previousUserTime = num3;
				if (num7 != 0L)
				{
					return Clamp((float)(num7 - num4) * 100f / (float)num7);
				}
			}
			return Clamp((cpuCounter == null) ? cpuUsage : cpuCounter.NextValue());
		}
		catch
		{
			return cpuUsage;
		}
	}

	private float ReadGpu()
	{
		try
		{
			if (!gpuCountersInitialized || gpuRefreshCountdown <= 0)
			{
				RefreshGpuCounters();
				gpuRefreshCountdown = 10;
			}
			gpuRefreshCountdown--;
			float num = 0f;
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, PerformanceCounter> gpuCounter in gpuCounters)
			{
				try
				{
					num += Math.Max(0f, gpuCounter.Value.NextValue());
				}
				catch
				{
					list.Add(gpuCounter.Key);
				}
			}
			foreach (string item in list)
			{
				((Component)(object)gpuCounters[item]).Dispose();
				gpuCounters.Remove(item);
			}
			return Clamp(num);
		}
		catch
		{
			return gpuUsage;
		}
	}

	private void RefreshGpuCounters()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		PerformanceCounterCategory val = new PerformanceCounterCategory("GPU Engine");
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string[] instanceNames = val.GetInstanceNames();
		foreach (string text in instanceNames)
		{
			if (text.IndexOf("engtype_", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				hashSet.Add(text);
				if (!gpuCounters.ContainsKey(text))
				{
					PerformanceCounter val2 = new PerformanceCounter("GPU Engine", "Utilization Percentage", text);
					val2.NextValue();
					gpuCounters[text] = val2;
				}
			}
		}
		List<string> list = new List<string>();
		foreach (string key in gpuCounters.Keys)
		{
			if (!hashSet.Contains(key))
			{
				list.Add(key);
			}
		}
		foreach (string item in list)
		{
			((Component)(object)gpuCounters[item]).Dispose();
			gpuCounters.Remove(item);
		}
		gpuCountersInitialized = true;
	}

	private static float ReadRam()
	{
		try
		{
			MEMORYSTATUSEX lpBuffer = new MEMORYSTATUSEX
			{
				dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX))
			};
			return GlobalMemoryStatusEx(ref lpBuffer) ? Clamp(lpBuffer.dwMemoryLoad) : 0f;
		}
		catch
		{
			return 0f;
		}
	}

	private void Push(string key, float value)
	{
		Queue<float> queue = history[key];
		while (queue.Count >= 60)
		{
			queue.Dequeue();
		}
		queue.Enqueue(value);
	}

	private static float Clamp(float value)
	{
		return Math.Max(0f, Math.Min(100f, value));
	}

	private static ulong ToUInt64(FILETIME value)
	{
		return ((ulong)value.dwHighDateTime << 32) | value.dwLowDateTime;
	}

	private static Color GetModeColor(string name)
	{
		if (name == "GPU")
		{
			return Color.FromArgb(55, 201, 255);
		}
		if (name == "RAM")
		{
			return Color.FromArgb(128, 105, 255);
		}
		return Color.FromArgb(64, 139, 255);
	}

	private static GraphicsPath Rounded(Rectangle rectangle, int radius)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		GraphicsPath val = new GraphicsPath();
		int num = Math.Max(2, radius * 2);
		val.AddArc(rectangle.Left, rectangle.Top, num, num, 180f, 90f);
		val.AddArc(rectangle.Right - num, rectangle.Top, num, num, 270f, 90f);
		val.AddArc(rectangle.Right - num, rectangle.Bottom - num, num, num, 0f, 90f);
		val.AddArc(rectangle.Left, rectangle.Bottom - num, num, num, 90f, 90f);
		val.CloseFigure();
		return val;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);
}
