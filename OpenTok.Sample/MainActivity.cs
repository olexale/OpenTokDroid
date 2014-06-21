using System.Collections.Generic;

using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

using Com.Opentok.Android;

namespace OpenTok.Sample
{
    [Activity (Label = "OpenTok.Sample", MainLauncher = true,
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class MainActivity : Activity, Session.ISessionListener, Publisher.IPublisherListener, Subscriber.IVideoListener
    {
        const string ApiKey = "";
        const string SessionId = "";
        const string Token = "";

        Session _session;
        Publisher _publisher;
        Subscriber _subscriber;
        List<Stream> _streams = new List<Stream> ();

        RelativeLayout _publisherViewContainer;
        RelativeLayout _subscriberViewContainer;
        // Spinning wheel for loading subscriber view
        ProgressBar _loadingSub;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            // Set our view from the "main" layout resource
            SetContentView (Resource.Layout.Main);

            _publisherViewContainer = FindViewById<RelativeLayout> (Resource.Id.publisherview);
            _subscriberViewContainer = FindViewById<RelativeLayout> (Resource.Id.subscriberview);
            _loadingSub = FindViewById<ProgressBar> (Resource.Id.loadingSpinner);

            SessionConnect (SessionId, Token);
        }

        protected override void OnStop ()
        {
            base.OnStop ();
            if (!IsFinishing)
                return;

            if (_session != null) {
                _session.Disconnect ();
            }
        }

        public override void OnBackPressed ()
        {
            if (_session != null) {
                _session.Disconnect ();
            }
            base.OnBackPressed ();
        }

        void SessionConnect (string sessionId, string token)
        {
            if (_session != null)
                return;

            _session = new Session (this, ApiKey, sessionId);
            _session.SetSessionListener (this);
            _session.Connect (token);
        }

        public void OnConnected (Session p0)
        {
            if (_publisher != null)
                return;

            _publisher = new Publisher (this, "publisher");
            _publisher.SetPublisherListener (this);
            AttachPublisherView (_publisher);
            p0.Publish (_publisher);
        }

        public void OnDisconnected (Session p0)
        {
            if (_publisher != null) {
                _publisherViewContainer.RemoveView (_publisher.View);
            }

            if (_subscriber != null) {
                _subscriberViewContainer.RemoveView (_subscriber.View);
            }

            _publisher = null;
            _subscriber = null;
            _streams.Clear ();
            _session = null;
        }

        void SubscribeToStream (Stream stream)
        {
            _subscriber = new Subscriber (this, stream);
            _subscriber.SetVideoListener (this);
            _session.Subscribe (_subscriber);
            // start loading spinning
            _loadingSub.Visibility = ViewStates.Visible;
        }

        void UnsubscribeFromStream (Stream stream)
        {
            _streams.Remove (stream);
            if (_subscriber.Stream.StreamId.Equals (stream.StreamId)) {
                _subscriberViewContainer.RemoveView (_subscriber.View);
                _subscriber = null;
                if (_streams.Count != 0) {
                    SubscribeToStream (_streams [0]);
                }
            }
        }

        void AttachSubscriberView (Subscriber subscriber)
        {
            var layoutParams = new RelativeLayout.LayoutParams (
                                   Resources.DisplayMetrics.WidthPixels, Resources.DisplayMetrics.HeightPixels);


            _subscriberViewContainer.AddView (_subscriber.View, layoutParams);
            subscriber.SetStyle (BaseVideoRenderer.StyleVideoScale,
                BaseVideoRenderer.StyleVideoFill);
        }

        void AttachPublisherView (Publisher publisher)
        {
            _publisher.SetStyle (BaseVideoRenderer.StyleVideoScale, BaseVideoRenderer.StyleVideoFill);
            var layoutParams = new RelativeLayout.LayoutParams (320, 240);
            layoutParams.AddRule (LayoutRules.AlignParentBottom, -1);
            layoutParams.AddRule (LayoutRules.AlignParentRight, -1);
            _publisherViewContainer.AddView (publisher.View, layoutParams);
        }

        public void OnError (Session p0, OpentokError p1)
        {
        }

        public void OnStreamDropped (Session p0, Stream p1)
        {
            if (_subscriber != null) {
                UnsubscribeFromStream (p1);
            }
        }

        public void OnStreamReceived (Session p0, Stream p1)
        {
            _streams.Add (p1);
            if (_subscriber == null) {
                SubscribeToStream (p1);
            }
        }

        public void OnError (PublisherKit p0, OpentokError p1)
        {
        }

        public void OnStreamCreated (PublisherKit p0, Stream p1)
        {
            _streams.Add (p1);
            if (_subscriber == null) {
                SubscribeToStream (p1);
            }
        }

        public void OnStreamDestroyed (PublisherKit p0, Stream p1)
        {
            if ((_subscriber != null)) {
                UnsubscribeFromStream (p1);
            }
        }

        public void OnVideoDataReceived (SubscriberKit p0)
        {
            _loadingSub.Visibility = ViewStates.Gone;
            AttachSubscriberView (_subscriber);
        }

        public void OnVideoDisabled (SubscriberKit p0)
        {
        }
    }
}
