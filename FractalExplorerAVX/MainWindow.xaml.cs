using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using FractalExplorer.Fractals;
using FractalExplorer.Numerics;
using FractalExplorer.Rendering;

namespace FractalExplorerAVX {

	public partial class MainWindow : Window {

		public MainWindow () {

			InitializeComponent ();
			IO_PreviewImage.Stretch = Stretch.Uniform;

			this.Closed += new EventHandler (OnExit);
			this.MouseWheel += new MouseWheelEventHandler (OnScroll);
			this.MouseMove += new MouseEventHandler (OnMouseMove);
			IO_PreviewImage.MouseLeftButtonDown += new MouseButtonEventHandler (LeftClickDown);
			IO_PreviewImage.MouseLeftButtonUp += new MouseButtonEventHandler (LeftClickUp);

			this.KeyDown += new KeyEventHandler (PressKey);

			Renderer.window = this;
			Renderer.Initialize ();
		}

		public void ExecuteOnMainThread (Action a) {
			Dispatcher.Invoke (a);
		}

		private void ExportImage (object sender, RoutedEventArgs e) {
			Renderer.RenderImage ();
		}

		private void ExportImageSequence (object sender, RoutedEventArgs e) {
			Renderer.RenderImageSequence ();
		}

		private void StopRendering (object sender, RoutedEventArgs e) {
			Renderer.StopRendering ();
		}

		private bool isHoldingDown = false;
		private Point oldMousePosition;
		private void LeftClickDown (object sender, MouseButtonEventArgs e) {
			isHoldingDown = e.LeftButton == MouseButtonState.Pressed;
			if (isHoldingDown)
				oldMousePosition = e.GetPosition (this);
		}

		private void LeftClickUp (object sender, MouseEventArgs e) {
			isHoldingDown = false;
		}

		private void OnMouseMove (object sender, MouseEventArgs e) {
			if (isHoldingDown) {
				Point p = e.GetPosition (IO_PreviewImage);
				double xMove = p.X - oldMousePosition.X;
				double yMove = p.Y - oldMousePosition.Y;
				Renderer.MoveFractalView (xMove, -yMove);
				oldMousePosition = p;
			}
		}

		private void PressKey (object sender, KeyEventArgs e) {
			if (e.Key == Key.LeftShift) {
				Renderer.ChangeFractalLimit (50);
			}
			if (e.Key == Key.LeftCtrl) {
				Renderer.ChangeFractalLimit (-50);
			}
		}

		private void OnScroll (object sender, MouseWheelEventArgs e) {
			Renderer.ZoomFractalView (e.Delta);
		}

		private void OnExit (object sender, EventArgs e) {
			// Renderer.AbortAllThreads ();
		}
		private uint[] resolutions = new uint[] {
		((uint)352 << 16) + 240,
		((uint)640 << 16) + 360,
		((uint)854 << 16) + 480,
		((uint)1280 << 16) + 720,
		((uint)1920 << 16) + 1080,
		((uint)2560 << 16) + 1440,
		((uint)3840 << 16) + 2160,
		((uint)7680 << 16) + 4320
		};
		private void ChangeResolution (object sender, SelectionChangedEventArgs e) {
			int width = (int)((resolutions[IO_ResolutionSelection.SelectedIndex] & 0xffff0000) >> 16);
			int height = (int)(resolutions[IO_ResolutionSelection.SelectedIndex] & 0x0000ffff);
			Renderer.SetRenderResolution (width, height);
		}
	}
}
