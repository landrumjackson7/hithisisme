using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AstryxTweaks;

public class ToggleSwitch : CheckBox
{
	private float animationProgress;

	public ToggleSwitch()
	{
		((Control)this).AutoSize = false;
		((Control)this).Size = new Size(48, 26);
		((Control)this).Text = "";
		((Control)this).Cursor = Cursors.Hand;
		SetStyle((ControlStyles)139282, true);
	}

	protected override void OnCreateControl()
	{
		base.OnCreateControl();
		animationProgress = (((CheckBox)this).Checked ? 1f : 0f);
	}

	protected override void OnCheckedChanged(EventArgs e)
	{
		base.OnCheckedChanged(e);
		animationProgress = (((CheckBox)this).Checked ? 1f : 0f);
		((Control)this).Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Expected O, but got Unknown
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Expected O, but got Unknown
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Expected O, but got Unknown
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Expected O, but got Unknown
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Expected O, but got Unknown
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Expected O, but got Unknown
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = (SmoothingMode)4;
		Rectangle rectangle = new Rectangle(1, 3, 45, 20);
		Color color = Color.FromArgb(55, 67, 91);
		Color to = Color.FromArgb(42, 119, 255);
		Color color2 = Blend(color, to, animationProgress);
		GraphicsPath val = Rounded(rectangle, 10);
		try
		{
			LinearGradientBrush val2 = new LinearGradientBrush(rectangle, Blend(color2, Color.White, ((CheckBox)this).Checked ? 0.14f : 0f), color2, (LinearGradientMode)2);
			try
			{
				Pen val3 = new Pen(Blend(Color.FromArgb(73, 86, 115), Color.FromArgb(130, 181, 255), animationProgress));
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
		float num = 4f + 21f * animationProgress;
		SolidBrush val4 = new SolidBrush(Color.FromArgb((int)(120f * animationProgress), 120, 190, 255));
		try
		{
			SolidBrush val5 = new SolidBrush(Color.White);
			try
			{
				graphics.FillEllipse((Brush)val4, num - 3f, 2f, 24f, 24f);
				graphics.FillEllipse((Brush)val5, num, 5f, 18f, 18f);
				SolidBrush val6 = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
				try
				{
					graphics.FillEllipse((Brush)val6, num + 3.5f, 6.5f, 10f, 6f);
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
	}

	private static Color Blend(Color from, Color to, float amount)
	{
		amount = Math.Max(0f, Math.Min(1f, amount));
		return Color.FromArgb((int)((float)(int)from.A + (float)(to.A - from.A) * amount), (int)((float)(int)from.R + (float)(to.R - from.R) * amount), (int)((float)(int)from.G + (float)(to.G - from.G) * amount), (int)((float)(int)from.B + (float)(to.B - from.B) * amount));
	}

	private static GraphicsPath Rounded(Rectangle rectangle, int radius)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected O, but got Unknown
		GraphicsPath val = new GraphicsPath();
		int num = radius * 2;
		val.AddArc(rectangle.Left, rectangle.Top, num, num, 180f, 90f);
		val.AddArc(rectangle.Right - num, rectangle.Top, num, num, 270f, 90f);
		val.AddArc(rectangle.Right - num, rectangle.Bottom - num, num, num, 0f, 90f);
		val.AddArc(rectangle.Left, rectangle.Bottom - num, num, num, 90f, 90f);
		val.CloseFigure();
		return val;
	}
}
