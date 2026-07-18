using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OWCE.Network;
using OWCE.Protobuf;
using Xamarin.Forms;

namespace OWCE.Pages
{
    public class ViewRawRideDataModel : Xamarin.CommunityToolkit.ObjectModel.ObservableObject
    {
        string _dataFile = String.Empty;
        public string DataFile
        {
            get => _dataFile;
            set => SetProperty(ref _dataFile, value);
        }

        SubmitRideRequest _submitRideRequest;
        public SubmitRideRequest SubmitRideRequest
        {
            get => _submitRideRequest;
            set => SetProperty(ref _submitRideRequest, value);
        }

        List<OWBoardEvent> _boardEvents = new List<OWBoardEvent>();
        public List<OWBoardEvent> BoardEvents
        {
            get => _boardEvents;
            set => SetProperty(ref _boardEvents, value);
        }


        WeakReference<ViewRawRideDataPage> _page;

        public ViewRawRideDataModel(ViewRawRideDataPage page)
        {
            _page = new WeakReference<ViewRawRideDataPage>(page);
        }

        internal void LoadData()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                var boardEvents = new List<OWBoardEvent>();
                using (var inputFile = new FileStream(DataFile, FileMode.Open, FileAccess.Read))
                {
                    do
                    {
                        var currentEvent = OWBoardEvent.Parser.ParseDelimitedFrom(inputFile);
                        boardEvents.Add(currentEvent);
                    }
                    while (inputFile.Position < inputFile.Length);
                }

                // BoardEvents is bound to ViewRawRideDataPage's CollectionView.ItemsSource -
                // raising its PropertyChanged off this background thread is unsafe (can
                // crash on iOS/Android), so marshal the assignment to the main thread.
                Device.BeginInvokeOnMainThread(() =>
                {
                    BoardEvents = null;
                    BoardEvents = boardEvents;
                });
            });
        }
    }
}

