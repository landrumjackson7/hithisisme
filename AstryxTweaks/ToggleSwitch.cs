using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AstryxTweaks;

public class ToggleSwitch : CheckBox
{
	private float animationProgress;
	private float animationStart;
	private float animationTarget;
	private DateTime animationStarted;
	private System.Windows.Forms.Timer animationTimer;
	private bool hovered;
	private bool pressed;

	public ToggleSwitch()
	{
		((Control)this).AutoSize = false;
		((Control)this).Size = new Size(48, 26);
		((Control)this).Text = "";
		((Control)this).Cursor = Cursors.Hand;
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
		((Control)this).BackColor = Color.Transparent;
	}

	protected override void OnCreateControl()
	{
		base.OnCreateControl();
		animationProgress = (((CheckBox)this).Checked ? 1f : 0f);
	}

	protected override void OnCheckedChanged(EventArgs e)
	{
		base.OnCheckedChanged(e);
		animationTarget = (((CheckBox)this).Checked ? 1f : 0f);
		if (!IsHandleCreated)
		{
			animationProgress = animationTarget;
			return;
		}
		animationStart = animationProgress;
		animationStarted = DateTime.UtcNow;
		if (animationTimer == null)
		{
			animationTimer = new System.Windows.Forms.Timer();
			animationTimer.Interval = 15;
			animationTimer.Tick += AnimateTick;
		}
		animationTimer.Stop();
		animationTimer.Start();
		((Control)this).Invalidate();
	}

	private void AnimateTick(object sender, EventArgs e)
	{
		float progress = (float)(DateTime.UtcNow - animationStarted).TotalMilliseconds / 170f;
		if (progress >= 1f)
		{
			animationProgress = animationTarget;
			animationTimer.Stop();
		}
		else
		{
			float eased = 1f - (float)Math.Pow(1f - Math.Max(0f, progress), 3.0);
			animationProgress = animationStart + (animationTarget - animationStart) * eased;
		}
		((Control)this).Invalidate();
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		hovered = true;
		base.OnMouseEnter(e);
		((Control)this).Invalidate();
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		hovered = false;
		pressed = false;
		base.OnMouseLeave(e);
		((Control)this).Invalidate();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		pressed = true;
		base.OnMouseDown(e);
		((Control)this).Invalidate();
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		pressed = false;
		base.OnMouseUp(e);
		((Control)this).Invalidate();
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		((Control)this).Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(((Control)this).Parent == null ? Color.FromArgb(13, 20, 38) : ((Control)this).Parent.BackColor);

		Rectangle track = new Rectangle(2, 4, 44, 18);
		Color offColor = Color.FromArgb(48, 58, 78);
		Color onColor = Color.FromArgb(51, 126, 241);
		Color trackColor = Blend(offColor, onColor, animationProgress);
		if (hovered)
		{
			trackColor = Blend(trackColor, Color.White, 0.08f);
		}
		if (!Enabled)
		{
			trackColor = Blend(trackColor, Color.FromArgb(34, 39, 52), 0.55f);
		}

		GraphicsPath path = Rounded(track, 9);
		try
		{
			SolidBrush trackBrush = new SolidBrush(trackColor);
			try
			{
				graphics.FillPath(trackBrush, path);
			}
			finally
			{
				trackBrush.Dispose();
			}
		}
		finally
		{
			path.Dispose();
		}

		float thumbX = 4f + 22f * animationProgress;
		float thumbY = pressed ? 6f : 5f;
		SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(42, 0, 0, 0));
		try
		{
			graphics.FillEllipse(shadowBrush, thumbX + 1f, thumbY + 2f, 16f, 16f);
		}
		finally
		{
			shadowBrush.Dispose();
		}
		SolidBrush thumbBrush = new SolidBrush(Enabled ? Color.FromArgb(248, 250, 255) : Color.FromArgb(166, 171, 184));
		try
		{
			graphics.FillEllipse(thumbBrush, thumbX, thumbY, 16f, 16f);
		}
		finally
		{
			thumbBrush.Dispose();
		}

		if (Focused && ShowFocusCues)
		{
			Pen focusPen = new Pen(Color.FromArgb(165, 148, 190, 255), 1f);
			GraphicsPath focusPath = Rounded(new Rectangle(1, 3, 46, 20), 10);
			try
			{
				graphics.DrawPath(focusPen, focusPath);
			}
			finally
			{
				focusPen.Dispose();
				focusPath.Dispose();
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && animationTimer != null)
		{
			animationTimer.Stop();
			animationTimer.Dispose();
			animationTimer = null;
		}
		base.Dispose(disposing);
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
