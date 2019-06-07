﻿using DocDigitFinal.DataModels;
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
            _session = new TwainSession(appId);
            _session.TransferError += _session_TransferError;
            _session.TransferReady += _session_TransferReady;
            _session.DataTransferred += _session_DataTransferred;
            _session.SourceDisabled += _session_SourceDisabled;
            _session.StateChanged += (s, e) => { RaisePropertyChanged(() => State); };
        }

        TwainSession _session;

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
                if (_session.State == 4)
                {
                    _session.CurrentSource.Close();
                }
                _selectedSource = value;
                RaisePropertyChanged(() => SelectedSource);
                if (_selectedSource != null)
                {
                    _selectedSource.Open();
                }
            }
        }

        public int State { get { return _session.State; } }

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
                    var rc = _session.Open(new WpfMessageLoopHook(value));

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
                    if (_session.State == 4)
                    {
                        var rc = _session.CurrentSource.Enable(SourceEnableMode.ShowUIOnly, false, WindowHandle);
                    }
                }, () =>
                {
                    return _session.State == 4 && _session.CurrentSource.Capabilities.CapEnableDSUIOnly.GetCurrent() == BoolType.True;
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
                    if (_session.State == 4)
                    {
                        var rc = _session.CurrentSource.Enable(ShowUI ? SourceEnableMode.ShowUI : SourceEnableMode.NoUI, false, WindowHandle);
                    }
                }, () =>
                {
                    return _session.State == 4;
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
                    foreach (var s in _session.Select(s => new DataSourceVM { DS = s }))
                    {
                        DataSources.Add(s);
                    }
                    if (DataSources.Count > 0) SelectedSource = DataSources[0];
                }, () =>
                {
                    return _session.State > 2;
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
                    // Confirming upload
                    await scannedDocument.Upload();
                    CapturedImages = new ObservableCollection<ImageSource>();
                    scannedDocument = null;
                }, () =>
                {
                    IsSendVisible = CapturedImages != null && SelectedStudent != null && SelectedDocument != null;
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

        void _session_SourceDisabled(object sender, EventArgs e)
        {
            Messenger.Default.Send(new RefreshCommandsMessage());
        }

        void _session_TransferError(object sender, TransferErrorEventArgs e)
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

        void _session_TransferReady(object sender, TransferReadyEventArgs e)
        {
            var mech = _session.CurrentSource.Capabilities.ICapXferMech.GetCurrent();
            if (mech == XferMech.File)
            {
                var formats = _session.CurrentSource.Capabilities.ICapImageFileFormat.GetValues();
                var wantFormat = formats.Contains(FileFormat.Tiff) ? FileFormat.Tiff : FileFormat.Bmp;

                var fileSetup = new TWSetupFileXfer
                {
                    Format = wantFormat,
                    FileName = GetUniqueName(Path.GetTempPath(), "twain-test", "." + wantFormat)
                };
                var rc = _session.CurrentSource.DGControl.SetupFileXfer.Set(fileSetup);
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

        void _session_DataTransferred(object sender, DataTransferredEventArgs e)
        {
            ImageSource img = GenerateThumbnail(e);
            if (img != null)
            {
                App.Current.Dispatcher.BeginInvoke(new Action(async () =>
                {
                    CapturedImages.Add(img);
                    SelectedImage = img;
                    if (IsSendVisible && scannedDocument == null)
                    {
                        scannedDocument = new ScannedDocument(CurrentUser.id, SelectedStudent.id, SelectedDocument.id);
                        await scannedDocument.InitScan();
                    }
                    if (scannedDocument != null)
                    {
                        await scannedDocument.AddPage(img);
                    }
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
            if (_session.State == 4)
            {
                _session.CurrentSource.Close();
            }
            _session.Close();
        }
    }
}
