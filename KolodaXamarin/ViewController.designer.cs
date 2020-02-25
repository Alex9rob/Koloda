// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace KolodaXamarin
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton _btnLeft { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton _btnRight { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton _btnUndo { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        Koloda.KolodaView _kolodaView { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (_btnLeft != null) {
                _btnLeft.Dispose ();
                _btnLeft = null;
            }

            if (_btnRight != null) {
                _btnRight.Dispose ();
                _btnRight = null;
            }

            if (_btnUndo != null) {
                _btnUndo.Dispose ();
                _btnUndo = null;
            }

            if (_kolodaView != null) {
                _kolodaView.Dispose ();
                _kolodaView = null;
            }
        }
    }
}