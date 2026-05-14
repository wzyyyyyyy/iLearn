using System.Windows.Media;
using System.Windows.Threading;

namespace iLearn
{
    public class EasterEggController
    {
        private class EasterEggInstance
        {
            public EasterEggWindow Window { get; }
            public double VX { get; set; }
            public double VY { get; set; }
            public double Mass { get; }
            public double Rotation { get; set; }
            public double VRotation { get; set; }
            public RotateTransform Transform { get; }

            public EasterEggInstance(EasterEggWindow window, double vx, double vy, double mass, double vRotation, RotateTransform transform)
            {
                Window = window;
                VX = vx;
                VY = vy;
                Mass = mass;
                Rotation = 0;
                VRotation = vRotation;
                Transform = transform;
            }
        }

        private Thread? _animationThread;
        private Dispatcher? _animationDispatcher;
        private static EasterEggController? _instance;
        private DateTime _lastFrameTime;

        public EasterEggController()
        {
            _instance = this;
        }

        public void Launch()
        {
            if (_animationThread != null && _animationThread.IsAlive) return;

            _animationThread = new Thread(AnimationThreadLoop) { IsBackground = true };
            _animationThread.SetApartmentState(ApartmentState.STA);
            _animationThread.Start();
        }

        private void AnimationThreadLoop()
        {
            _animationDispatcher = Dispatcher.CurrentDispatcher;

            var windowInstances = new List<EasterEggInstance>();
            var rand = new Random();

            // --- 物理参数 ---
            const int Count = 8;                          // 彩蛋窗口的数量
            const double SpeedBase = 150;                 // 移动速度基准 (像素/秒)
            const double RotationSpeedBase = 120;         // 旋转速度基准 (角度/秒)
            const double MinScale = 0.5;                  // 最小缩放比例
            const double MaxScale = 3.3;                  // 最大缩放比例

            double screenW = SystemParameters.VirtualScreenWidth;
            double screenH = SystemParameters.VirtualScreenHeight;
            double screenL = SystemParameters.VirtualScreenLeft;
            double screenT = SystemParameters.VirtualScreenTop;

            for (int i = 0; i < Count; i++)
            {
                double scale = MinScale + rand.NextDouble() * (MaxScale - MinScale);
                double w = EasterEggWindow.BaseSize * scale;
                double h = EasterEggWindow.BaseSize * scale;
                double x = screenL + rand.NextDouble() * (screenW - w);
                double y = screenT + rand.NextDouble() * (screenH - h);
                double speedFactor = SpeedBase / scale;
                double vx = speedFactor * (rand.NextDouble() * 2 - 1);
                double vy = speedFactor * (rand.NextDouble() * 2 - 1);
                double mass = w * h;
                double vRotation = (rand.NextDouble() * 2 - 1) * RotationSpeedBase;

                var window = new EasterEggWindow(w, h);
                var rotateTransform = new RotateTransform();
                window.TransformTarget.RenderTransform = rotateTransform;
                window.Left = x;
                window.Top = y;
                window.Show();

                if (i == Count - 1)
                {
                    window.Activate();
                }

                windowInstances.Add(new EasterEggInstance(window, vx, vy, mass, vRotation, rotateTransform));
            }

            _lastFrameTime = DateTime.Now;
            EventHandler renderingHandler = (s, e) =>
            {
                var now = DateTime.Now;
                double deltaTime = (now - _lastFrameTime).TotalSeconds;
                _lastFrameTime = now;

                UpdatePhysics(windowInstances, rand, screenL, screenT, screenW, screenH, deltaTime);
            };

            CompositionTarget.Rendering += renderingHandler;

            _animationDispatcher.ShutdownStarted += (s, e) =>
            {
                CompositionTarget.Rendering -= renderingHandler;
            };

            Dispatcher.Run();

            foreach (var instance in windowInstances)
            {
                instance.Window.Close();
            }
        }

        private void UpdatePhysics(List<EasterEggInstance> instances, Random rand, double screenL, double screenT, double screenW, double screenH, double deltaTime)
        {
            if (deltaTime <= 0) return;

            foreach (var instance in instances)
            {
                instance.Window.Left += instance.VX * deltaTime;
                instance.Window.Top += instance.VY * deltaTime;
                instance.Rotation += instance.VRotation * deltaTime;
                instance.Transform.Angle = instance.Rotation;

                if (instance.Window.Left < screenL) { instance.Window.Left = screenL; instance.VX = Math.Abs(instance.VX); }
                else if (instance.Window.Left + instance.Window.Width > screenL + screenW) { instance.Window.Left = screenL + screenW - instance.Window.Width; instance.VX = -Math.Abs(instance.VX); }

                if (instance.Window.Top < screenT) { instance.Window.Top = screenT; instance.VY = Math.Abs(instance.VY); }
                else if (instance.Window.Top + instance.Window.Height > screenT + screenH) { instance.Window.Top = screenT + screenH - instance.Window.Height; instance.VY = -Math.Abs(instance.VY); }
            }

            for (int i = 0; i < instances.Count - 1; i++)
            {
                for (int j = i + 1; j < instances.Count; j++)
                {
                    var a = instances[i];
                    var b = instances[j];
                    if (new Rect(a.Window.Left, a.Window.Top, a.Window.Width, a.Window.Height)
                        .IntersectsWith(new Rect(b.Window.Left, b.Window.Top, b.Window.Width, b.Window.Height)))
                    {
                        HandleElasticCollision(a, b, rand);
                    }
                }
            }
        }

        private void HandleElasticCollision(EasterEggInstance a, EasterEggInstance b, Random rand)
        {
            var centerA = new Point(a.Window.Left + a.Window.Width / 2, a.Window.Top + a.Window.Height / 2);
            var centerB = new Point(b.Window.Left + b.Window.Width / 2, b.Window.Top + b.Window.Height / 2);

            var collisionNormal = new Vector(centerB.X - centerA.X, centerB.Y - centerA.Y);
            if (collisionNormal.Length == 0) return;
            collisionNormal.Normalize();
            var collisionTangent = new Vector(-collisionNormal.Y, collisionNormal.X);

            double vA_normal = Vector.Multiply(new Vector(a.VX, a.VY), collisionNormal);
            double vA_tangent = Vector.Multiply(new Vector(a.VX, a.VY), collisionTangent);
            double vB_normal = Vector.Multiply(new Vector(b.VX, b.VY), collisionNormal);
            double vB_tangent = Vector.Multiply(new Vector(b.VX, b.VY), collisionTangent);

            double vA_normal_new = (vA_normal * (a.Mass - b.Mass) + 2 * b.Mass * vB_normal) / (a.Mass + b.Mass);
            double vB_normal_new = (vB_normal * (b.Mass - a.Mass) + 2 * a.Mass * vA_normal) / (a.Mass + b.Mass);

            Vector vA_final = vA_normal_new * collisionNormal + vA_tangent * collisionTangent;
            Vector vB_final = vB_normal_new * collisionNormal + vB_tangent * collisionTangent;

            a.VX = vA_final.X;
            a.VY = vA_final.Y;
            b.VX = vB_final.X;
            b.VY = vB_final.Y;

            a.VRotation += (rand.NextDouble() - 0.5) * 60;
            b.VRotation += (rand.NextDouble() - 0.5) * 60;

            double overlap = 1.0;
            a.Window.Left -= collisionNormal.X * overlap;
            a.Window.Top -= collisionNormal.Y * overlap;
            b.Window.Left += collisionNormal.X * overlap;
            b.Window.Top += collisionNormal.Y * overlap;
        }

        public static void RequestExit()
        {
            _instance?._animationDispatcher?.InvokeShutdown();
            _instance = null;
        }
    }
}
