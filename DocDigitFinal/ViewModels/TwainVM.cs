using DocDigitFinal.DataModels;
using DocDigitFinal.ViewModels;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using ModernWpf.Messages;
using NTwain;
using NTwain.Data;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DocDigitFinal
{
    /// <summary>
    /// Wraps the twain session as a view model for databinding.
    /// </summary>
    class TwainVM : ViewModelBase
    {
        public TwainVM()
        {
            DataSources = new ObservableCollection<DataSourceVM>();
            CapturedImages = new ObservableCollection<ImageSource>();

            //this.SynchronizationContext = SynchronizationContext.Current;
            var appId = TWIdentity.CreateFromAssembly(DataGroups.Image | DataGroups.Audio, Assembly.GetEntryAssembly());
            session = new TwainSession(appId);
            session.TransferError += Session_TransferError;
            session.TransferReady += Session_TransferReady;
            session.DataTransferred += Session_DataTransferred;
            session.SourceDisabled += Session_SourceDisabled;
            session.StateChanged += (s, e) => { RaisePropertyChanged(() => State); };
        }

        TwainSession session;

        #region properties
        private ScannedDocument scannedDocument;
        public ObservableCollection<DataSourceVM> DataSources { get; private set; }

        private ImageSource _selectedImage;
        public ImageSource SelectedImage
        {
            get { return _selectedImage; }
            set
            {
                _selectedImage = value;
                RaisePropertyChanged(() => SelectedImage);
            }
        }

        private Student _selectedStudent;
        public Student SelectedStudent
        {
            get { return _selectedStudent; }
            set
            {
                _selectedStudent = value;
                if (scannedDocument != null) scannedDocument.StudentId = _selectedStudent.id;
                RaisePropertyChanged(() => SelectedStudent);
            }
        }

        private DocType _selectedDocument;
        public DocType SelectedDocument
        {
            get { return _selectedDocument; }
            set
            {
                _selectedDocument = value;
                if (scannedDocument != null) scannedDocument.DocumentId = _selectedDocument.id;
                RaisePropertyChanged(() => SelectedDocument);
            }
        }

        private User _currentUser;
        public User CurrentUser
        {
            get { return _currentUser; }
            set
            {
                _currentUser = value;
                RaisePropertyChanged(() => CurrentUser);
            }
        }

         public ObservableCollection<DocType> _docTypes;
        public ObservableCollection<DocType> DocTypes {
            get { return _docTypes; }
            set
            {
                _docTypes = value;
                RaisePropertyChanged(() => DocTypes);
            }
        }

        public ObservableCollection<Student> _students;
        public ObservableCollection<Student> Students {
            get { return _students; }
            set
            {
                _students = value;
                RaisePropertyChanged(() => Students);
            }
        }

        private DataSourceVM _selectedSource;
        public DataSourceVM SelectedSource
        {
            get { return _selectedSource; }
            set
            {
                if (session.State == 4)
                {
                    session.CurrentSource.Close();
                }
                _selectedSource = value;
                RaisePropertyChanged(() => SelectedSource);
                if (_selectedSource != null)
                {
                    _selectedSource.Open();
                }
            }
        }

        public int State { get { return session.State; } }

        private IntPtr _winHandle;
        public IntPtr WindowHandle
        {
            get { return _winHandle; }
            set
            {
                _winHandle = value;
                if (value == IntPtr.Zero)
                {

                }
                else
                {
                    var rc = session.Open(new WpfMessageLoopHook(value));

                    if (rc == ReturnCode.Success)
                    {
                        ReloadSourcesCommand.Execute(null);
                    }
                }
            }
        }

        public bool ShowUI
        {
            get; set;
        }


        private ICommand _showDriverCommand;
        public ICommand ShowDriverCommand
        {
            get
            {
                return _showDriverCommand ?? (_showDriverCommand = new RelayCommand(() =>
                {
                    if (session.State == 4)
                    {
                        var rc = session.CurrentSource.Enable(SourceEnableMode.ShowUIOnly, false, WindowHandle);
                    }
                }, () =>
                {
                    return session.State == 4 && session.CurrentSource.Capabilities.CapEnableDSUIOnly.GetCurrent() == BoolType.True;
                }));
            }
        }

        private ICommand _captureCommand;
        public ICommand CaptureCommand
        {
            get
            {
                return _captureCommand ?? (_captureCommand = new RelayCommand(() =>
                {
                    if (session.State == 4)
                    {
                        var rc = session.CurrentSource.Enable(ShowUI ? SourceEnableMode.ShowUI : SourceEnableMode.NoUI, false, WindowHandle);
                    }
                }, () =>
                {
                    return session.State == 4;
                }));
            }
        }

        private ICommand _clearCommand;
        public ICommand ClearCommand
        {
            get
            {
                return _clearCommand ?? (_clearCommand = new RelayCommand(() =>
                {
                    CapturedImages.Clear();
                }, () =>
                {
                    return CapturedImages.Count > 0;
                }));
            }
        }

        private ICommand _reloadSrc;
        public ICommand ReloadSourcesCommand
        {
            get
            {
                return _reloadSrc ?? (_reloadSrc = new RelayCommand(() =>
                {
                    DataSources.Clear();
                    foreach (var s in session.Select(s => new DataSourceVM { DS = s }))
                    {
                        DataSources.Add(s);
                    }
                    if (DataSources.Count > 0) SelectedSource = DataSources[0];
                }, () =>
                {
                    return session.State > 2;
                }));
            }
        }

        private ICommand _rotateLeftCommand;
        public ICommand RotateLeftCommand
        {
            get
            {
                return _rotateLeftCommand ?? (_rotateLeftCommand = new RelayCommand(() =>
                {
                    TransformedBitmap img = new TransformedBitmap((BitmapSource)SelectedImage, new RotateTransform(-90));
                    CapturedImages[CapturedImages.IndexOf(SelectedImage)] = img;
                    SelectedImage = img;
                }, () =>
                {
                    return _selectedImage != null;
                }));
            }
        }

        private ICommand _rotateRightCommand;
        public ICommand RotateRightCommand
        {
            get
            {
                return _rotateRightCommand ?? (_rotateRightCommand = new RelayCommand(() =>
                {
                    TransformedBitmap img = new TransformedBitmap((BitmapSource)SelectedImage, new RotateTransform(90));
                    CapturedImages[CapturedImages.IndexOf(SelectedImage)] = img;
                    SelectedImage = img;
                }, () =>
                {
                    return _selectedImage != null;
                }));
            }
        }

        private ICommand _moveUpCommand;
        public ICommand MoveUpCommand
        {
            get
            {
                return _moveUpCommand ?? (_moveUpCommand = new RelayCommand(() =>
                {
                    var selectedImgIndex = CapturedImages.IndexOf(SelectedImage);
                    if (selectedImgIndex != 0)
                    {
                        var tmpImg = CapturedImages[selectedImgIndex - 1];
                        CapturedImages[selectedImgIndex - 1] = SelectedImage;
                        CapturedImages[selectedImgIndex] = tmpImg;
                        SelectedImage = CapturedImages[selectedImgIndex - 1];
                    }
                }, () =>
                {
                    return _selectedImage != null;
                }));
            }
        }

        private ICommand _moveDownCommand;
        public ICommand MoveDownCommand
        {
            get
            {
                return _moveDownCommand ?? (_moveDownCommand = new RelayCommand(() =>
                {
                    var selectedImgIndex = CapturedImages.IndexOf(SelectedImage);
                    if (selectedImgIndex != CapturedImages.Count - 1)
                    {
                        var tmpImg = CapturedImages[selectedImgIndex + 1];
                        CapturedImages[selectedImgIndex + 1] = SelectedImage;
                        CapturedImages[selectedImgIndex] = tmpImg;
                        SelectedImage = CapturedImages[selectedImgIndex + 1];
                    }
                }, () =>
                {
                    return _selectedImage != null;
                }));
            }
        }

        private ICommand _removeSelectedCommand;
        public ICommand RemoveSelectedCommand
        {
            get
            {
                return _removeSelectedCommand ?? (_removeSelectedCommand = new RelayCommand(() =>
                {
                    CapturedImages.RemoveAt(CapturedImages.IndexOf(SelectedImage));
                    if (CapturedImages.Count > 0) SelectedImage = CapturedImages.Last();
                    // else call api cancel 
                }, () =>
                {
                    return _selectedImage != null;
                }));
            }
        }

        private ICommand _sendCommand;
        public bool IsSendVisible;
        public ICommand SendCommand
        {
            get
            {
                return _sendCommand ?? (_sendCommand = new RelayCommand(async () =>
                {
                    string messageBoxText = "Czy zakończyć skanowanie i przesłać dokument?";
                    string caption = "Zakończ skanowanie";
                    MessageBoxButton button = MessageBoxButton.YesNo;
                    MessageBoxImage icon = MessageBoxImage.Warning;
                    var result = MessageBox.Show(messageBoxText, caption, button, icon);
                    if (result == MessageBoxResult.Yes)
                    {
                        RaisePropertyChanged("Upload");
                        await scannedDocument.CreatePDF(CapturedImages);
                        CapturedImages.Clear();
                        scannedDocument = null;
                        RaisePropertyChanged("Upload");
                    }
                }, () =>
                {
                    IsSendVisible = CapturedImages != null && CapturedImages.Count > 0 && SelectedStudent != null && SelectedDocument != null;
                    if (IsSendVisible && scannedDocument == null) scannedDocument = new ScannedDocument(CurrentUser.id, SelectedStudent.id, SelectedDocument.id);
                    return IsSendVisible;
                }));
            }
        }

        /// <summary>
        /// Gets the captured images.
        /// </summary>
        /// The captured images.
        /// <value>
        /// </value>
        public ObservableCollection<ImageSource> CapturedImages { get; private set; }

        public double MinThumbnailSize { get { return 50; } }
        public double MaxThumbnailSize { get { return 300; } }

        private double _thumbSize = 150;
        public double ThumbnailSize
        {
            get { return _thumbSize; }
            set
            {
                if (value > MaxThumbnailSize) { value = MaxThumbnailSize; }
                else if (value < MinThumbnailSize) { value = MinThumbnailSize; }
                _thumbSize = value;
                RaisePropertyChanged(() => ThumbnailSize);
            }
        }

        #endregion

        void Session_SourceDisabled(object sender, EventArgs e)
        {
            Messenger.Default.Send(new RefreshCommandsMessage());
        }

        void Session_TransferError(object sender, TransferErrorEventArgs e)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.Exception != null)
                {
                    Messenger.Default.Send(new MessageBoxMessage(e.Exception.Message, null)
                    {
                        Caption = "Transfer Error Exception",
                        Icon = System.Windows.MessageBoxImage.Error,
                        Button = System.Windows.MessageBoxButton.OK
                    });
                }
                else
                {
                    Messenger.Default.Send(new MessageBoxMessage(string.Format("Return Code: {0}\nCondition Code: {1}", e.ReturnCode, e.SourceStatus.ConditionCode), null)
                    {
                        Caption = "Transfer Error",
                        Icon = System.Windows.MessageBoxImage.Error,
                        Button = System.Windows.MessageBoxButton.OK
                    });
                }
            }));
        }

        void Session_TransferReady(object sender, TransferReadyEventArgs e)
        {
            var mech = session.CurrentSource.Capabilities.ICapXferMech.GetCurrent();
            if (mech == XferMech.File)
            {
                var formats = session.CurrentSource.Capabilities.ICapImageFileFormat.GetValues();
                var wantFormat = formats.Contains(FileFormat.Tiff) ? FileFormat.Tiff : FileFormat.Bmp;

                var fileSetup = new TWSetupFileXfer
                {
                    Format = wantFormat,
                    FileName = GetUniqueName(Path.GetTempPath(), "twain-test", "." + wantFormat)
                };
                session.CurrentSource.DGControl.SetupFileXfer.Set(fileSetup);
            }
        }

        string GetUniqueName(string dir, string name, string ext)
        {
            var filePath = Path.Combine(dir, name + ext);
            int next = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(dir, string.Format("{0} ({1}){2}", name, next++, ext));
            }
            return filePath;
        }

        void Session_DataTransferred(object sender, DataTransferredEventArgs e)
        {
            ImageSource img = GenerateThumbnail(e);
            if (img != null)
            {
                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CapturedImages.Add(img);
                    SelectedImage = img;
                    ScannedDocument.UploadQueue.Add(img);
                }));
            }
        }

        ImageSource GenerateThumbnail(DataTransferredEventArgs e)
        {
            BitmapSource img = null;

            switch (e.TransferType)
            {
                case XferMech.Native:
                    using (var stream = e.GetNativeImageStream())
                    {
                        if (stream != null)
                        {
                            // file size
                            img = stream.ConvertToWpfBitmap(1536, 0);
                        }
                    }
                    break;
                case XferMech.File:
                    img = new BitmapImage(new Uri(e.FileDataPath));
                    if (img.CanFreeze)
                    {
                        img.Freeze();
                    }
                    break;
                case XferMech.Memory:
                    // TODO: build current image from multiple data-xferred event
                    break;
            }
            return img;
        }

        internal void CloseDown()
        {
            if (session.State == 4)
            {
                session.CurrentSource.Close();
            }
            session.Close();
        }
    }
}
