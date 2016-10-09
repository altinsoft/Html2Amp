﻿using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using ComboRox.Core.Utilities.SimpleGuard;
using System;
using System.Text;

namespace Html2Amp.Sanitization.Implementation
{
    public class IFrameSanitizer : Sanitizer
    {
        public override bool CanSanitize(IElement element)
        {
            return element != null 
                && element is IHtmlInlineFrameElement 
                && this.IsValidSourceAttribute(element);
        }

        public override IElement Sanitize(IDocument document, IElement htmlElement)
        {
            Guard.Requires(document, "document").IsNotNull();
            Guard.Requires(htmlElement, "htmlElement").IsNotNull();

            var iframeElement = (IHtmlInlineFrameElement)htmlElement;
            this.SetSourceAttribute(iframeElement);

            var ampElement = document.CreateElement("amp-iframe");
            iframeElement.CopyTo(ampElement);

            this.SetElementLayout(iframeElement, ampElement);
            this.SetMediaElementLayout(iframeElement, ampElement);

            iframeElement.Parent.ReplaceChild(ampElement, iframeElement);

            return ampElement;
        }

        private void SetSourceAttribute(IHtmlInlineFrameElement iframeElement)
        {
            //Resources can be requested only via HTTPS
            var iframeElementSrc = new Uri(iframeElement.Source);

            if (iframeElementSrc.Scheme != "https")
            {
                var newUrl = new StringBuilder("https://");
                newUrl.Append(iframeElementSrc.Host);

                if (!iframeElementSrc.IsDefaultPort)
                {
                    newUrl.Append(":");
                    newUrl.Append(iframeElementSrc.Port);
                }

                newUrl.Append(iframeElementSrc.PathAndQuery);

                iframeElement.Source = newUrl.ToString();
            }
        }

        private bool IsValidSourceAttribute(IElement htmlElement)
        {
            var source = new Uri(htmlElement.GetAttribute("src"));
            var sandbox = htmlElement.GetAttribute("sandbox");

            //iframes could not be in the same origin as the container, unless they do not specify allow-same-origin.
            if (this.Configuration != null && !string.IsNullOrEmpty(this.Configuration.RelativeUrlsHost))
            {
                var urlsHost = new Uri(this.Configuration.RelativeUrlsHost);

                if (urlsHost.Host == source.Host)
                {
                    if (!string.IsNullOrEmpty(sandbox) && sandbox.Contains("allow-same-origin"))
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }

            return false;
        }
    }
}