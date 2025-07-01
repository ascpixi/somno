using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno.UI
{
    /// <summary>
    /// Represents an object that is capable of rendering to a screen overlay.
    /// </summary>
    internal interface IOverlayRenderable
    {
        /// <summary>
        /// Handles drawing to the calling overlay.
        /// </summary>
        /// <param name="overlay">The calling overlay.</param>
        public void RenderOnOverlay(SomnoOverlay overlay);

        /// <summary>
        /// Called when the overlay is being destroyed.
        /// </summary>
        public void OnOverlayDestroy() { }
    }
}
