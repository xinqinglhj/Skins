﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RcisSchoolBell.Animations;
using RcisSchoolBell.lib.MaterialSkin;

namespace RcisSchoolBell.Controls
{
    public class MaterialRadioButton : RadioButton, IMaterialControl
    {
        [Browsable(false)]
        public int Depth { get; set; }
        [Browsable(false)]
        public MaterialSkinManager SkinManager { get { return MaterialSkinManager.Instance; } }
        [Browsable(false)]
        public MouseState MouseState { get; set; }
        [Browsable(false)]
        public Point MouseLocation { get; set; }

        private bool _ripple;
        [Category("Behavior")]
        public bool Ripple
        {
            get { return _ripple; }
            set
            {
                _ripple = value;
                AutoSize = AutoSize; //Make AutoSize directly set the bounds.

                if (value)
                {
                    Margin = new Padding(0);
                }

                Invalidate();
            }
        }

		// animation managers
        private readonly AnimationManager _animationManager;
        private readonly AnimationManager _rippleAnimationManager;

		// size related variables which should be recalculated onsizechanged
		private Rectangle _radioButtonBounds;
        private int _boxOffset;

		// size constants
		private const int RadiobuttonSize = 19;
		private const int RadiobuttonSizeHalf = RadiobuttonSize / 2;
		private const int RadiobuttonOuterCircleWidth = 2;
		private const int RadiobuttonInnerCircleSize = RadiobuttonSize - (2 * RadiobuttonOuterCircleWidth);

        public MaterialRadioButton()
		{
			SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);

            _animationManager = new AnimationManager
            {
                AnimationType = AnimationType.EaseInOut,
                Increment = 0.06
            };
            _rippleAnimationManager = new AnimationManager(false)
            {
                AnimationType = AnimationType.Linear,
                Increment = 0.10,
                SecondaryIncrement = 0.08
            };
            _animationManager.OnAnimationProgress += sender => Invalidate();
            _rippleAnimationManager.OnAnimationProgress += sender => Invalidate();

            CheckedChanged += (sender, args) => _animationManager.StartNewAnimation(Checked ? AnimationDirection.In : AnimationDirection.Out);
			
            SizeChanged += OnSizeChanged;

            Ripple = true;
            MouseLocation = new Point(-1, -1);
        }
        private void OnSizeChanged(object sender, EventArgs eventArgs)
        {
            _boxOffset = Height / 2 - (int)Math.Ceiling(RadiobuttonSize / 2d);
            _radioButtonBounds = new Rectangle(_boxOffset, _boxOffset, RadiobuttonSize, RadiobuttonSize);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int width = _boxOffset + 20 + (int)CreateGraphics().MeasureString(Text, SkinManager.RobotoMedium10).Width;
            return Ripple ? new Size(width, 30) : new Size(width, 20);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            // clear the control
            g.Clear(Parent.BackColor);

            var radiobuttonCenter = _boxOffset + RadiobuttonSizeHalf;

            var animationProgress = _animationManager.GetProgress();

            int colorAlpha = Enabled ? (int)(animationProgress * 255.0) : SkinManager.GetCheckBoxOffDisabledColor().A;
            int backgroundAlpha = Enabled ? (int)(SkinManager.GetCheckboxOffColor().A * (1.0 - animationProgress)) : SkinManager.GetCheckBoxOffDisabledColor().A;
            float animationSize = (float)(animationProgress * 8f);
            float animationSizeHalf = animationSize / 2;
            animationSize = (float)(animationProgress * 9f);

			var brush = new SolidBrush(Color.FromArgb(colorAlpha, Enabled ? SkinManager.ColorScheme.AccentColor : SkinManager.GetCheckBoxOffDisabledColor()));
            var pen = new Pen(brush.Color);

            // draw ripple animation
            if (Ripple && _rippleAnimationManager.IsAnimating())
            {
                for (int i = 0; i < _rippleAnimationManager.GetAnimationCount(); i++)
                {
                    var animationValue = _rippleAnimationManager.GetProgress(i);
                    var animationSource = new Point(radiobuttonCenter, radiobuttonCenter);
                    var rippleBrush = new SolidBrush(Color.FromArgb((int)((animationValue * 40)), ((bool)_rippleAnimationManager.GetData(i)[0]) ? Color.Black : brush.Color));
                    var rippleHeight = (Height % 2 == 0) ? Height - 3 : Height - 2;
                    var rippleSize = (_rippleAnimationManager.GetDirection(i) == AnimationDirection.InOutIn) ? (int)(rippleHeight * (0.8d + (0.2d * animationValue))) : rippleHeight;
                    using (var path = DrawHelper.CreateRoundRect(animationSource.X - rippleSize / 2, animationSource.Y - rippleSize / 2, rippleSize, rippleSize, rippleSize / 2))
                    {
                        g.FillPath(rippleBrush, path);
                    }

                    rippleBrush.Dispose();
                }
            }

            // draw radiobutton circle
            Color uncheckedColor = DrawHelper.BlendColor(Parent.BackColor, Enabled ? SkinManager.GetCheckboxOffColor() : SkinManager.GetCheckBoxOffDisabledColor(), backgroundAlpha);

            using (var path = DrawHelper.CreateRoundRect(_boxOffset, _boxOffset, RadiobuttonSize, RadiobuttonSize, 9f))
            {
                g.FillPath(new SolidBrush(uncheckedColor), path);

	            if (Enabled)
	            {
                    g.FillPath(brush, path);
	            }
            }

            g.FillEllipse(
                new SolidBrush(Parent.BackColor), 
                RadiobuttonOuterCircleWidth + _boxOffset, 
                RadiobuttonOuterCircleWidth + _boxOffset, 
                RadiobuttonInnerCircleSize,
                RadiobuttonInnerCircleSize);

            if (Checked)
            {
                using (var path = DrawHelper.CreateRoundRect(radiobuttonCenter - animationSizeHalf, radiobuttonCenter - animationSizeHalf, animationSize, animationSize, 4f))
                {
                    g.FillPath(brush, path);
                }
            }
            SizeF stringSize = g.MeasureString(Text, SkinManager.RobotoMedium10);
            g.DrawString(Text, SkinManager.RobotoMedium10, Enabled ? SkinManager.GetPrimaryTextBrush() : SkinManager.GetDisabledOrHintBrush(), _boxOffset + 22, Height / 2 - stringSize.Height / 2);

            brush.Dispose();  
            pen.Dispose();
        }

        private bool IsMouseInCheckArea()
        {
            return _radioButtonBounds.Contains(MouseLocation);
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            Font = SkinManager.RobotoMedium10;

            if (DesignMode) return;

            MouseState = MouseState.Out;
            MouseEnter += (sender, args) =>
            {
                MouseState = MouseState.Hover;
            };
            MouseLeave += (sender, args) =>
            {
                MouseLocation = new Point(-1, -1);
                MouseState = MouseState.Out;
            };
            MouseDown += (sender, args) =>
            {
                MouseState = MouseState.Down;

                if (Ripple && args.Button == MouseButtons.Left && IsMouseInCheckArea())
                {
                    _rippleAnimationManager.SecondaryIncrement = 0;
                    _rippleAnimationManager.StartNewAnimation(AnimationDirection.InOutIn, new object[] { Checked });
                }
            };
            MouseUp += (sender, args) =>
            {
                MouseState = MouseState.Hover;
                _rippleAnimationManager.SecondaryIncrement = 0.08;
            };
            MouseMove += (sender, args) =>
            {
                MouseLocation = args.Location;
                Cursor = IsMouseInCheckArea() ? Cursors.Hand : Cursors.Default;
            };
        }
    }
}