﻿using System.Windows.Forms;
using System.Windows;
using FlickrNet;
using System;

namespace Epicture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            switch (Managers.Instance.service)
            {
                case SERVICE.FLICKR:
                    Managers.Instance.flicker.Connect();
                    break;
                case SERVICE.IMGUR:
                    Managers.Instance.imgur.Connect();
                    break;
            }
            Managers.Instance.cache.LoadFavorite();
            Managers.Instance.cache.LoadIndesirable();

            ActionPannel.Height = new GridLength(0);

            SearchRecent();
        }

        public bool CheckSize(Photo photo)
        {
            if (AllSize.IsChecked == true || (LargeSearch.IsChecked == true && photo.LargeUrl != null) || (MediumSearch.IsChecked == true && photo.MediumUrl != null) || (SmallSearch.IsChecked == true && photo.SmallUrl != null))
                return true;
            return false;
        }

        public void SearchRecent()
        {
            SearchMode();
            Pannel.Children.Clear();

            switch (Managers.Instance.service)
            {
                case SERVICE.FLICKR:
                    PhotoCollection photos = Managers.Instance.flicker.flickr.PhotosGetRecent(Managers.Instance.nav.Page, Managers.Instance.nav.ImagePerPage, PhotoSearchExtras.Tags | PhotoSearchExtras.Description | PhotoSearchExtras.LargeUrl | PhotoSearchExtras.MediumUrl | PhotoSearchExtras.SmallUrl);

                    foreach (Photo photo in photos)
                    {
                        if (!Managers.Instance.user.AllowIndesirable)
                        {
                            if (!Managers.Instance.cache.IsIndesirable(photo.PhotoId))
                                if (CheckSize(photo))
                                    LoadImage(photo);
                        }
                        else if (CheckSize(photo))
                            LoadImage(photo);
                    }
                    break;
                case SERVICE.IMGUR:
                    break;
            }

            ScrollPannel.ScrollToTop();
        }

        public void Search(string searchTerm, int numPage, int imagePerPage)
        {
            if (String.IsNullOrEmpty(searchTerm))
            {
                SearchRecent();
                return;
            }
            SearchMode();

            switch (Managers.Instance.service)
            {
                case SERVICE.FLICKR:
                    var options = new PhotoSearchOptions { Text = searchTerm, PerPage = imagePerPage, Page = numPage, SafeSearch = SafetyLevel.Restricted, Extras = PhotoSearchExtras.Description | PhotoSearchExtras.Usage | PhotoSearchExtras.LargeUrl | PhotoSearchExtras.MediumUrl | PhotoSearchExtras.SmallUrl };

                    PhotoCollection photos = Managers.Instance.flicker.flickr.PhotosSearch(options);
                    Pannel.Children.Clear();
                    foreach (Photo photo in photos)
                    {
                        if (!Managers.Instance.user.AllowIndesirable)
                        {
                            if (!Managers.Instance.cache.IsIndesirable(photo.PhotoId))
                                if (CheckSize(photo))
                                    LoadImage(photo);
                        }
                        else if (CheckSize(photo))
                            LoadImage(photo);
                    }
                    break;
                case SERVICE.IMGUR:
                    break;
            }
            
            ScrollPannel.ScrollToTop();
        }

        public void LoadImage(Photo photo)
        {
            ImageInfo imgProfil = new ImageInfo(photo);
            Pannel.Children.Add(imgProfil);
        }

        private void NextPage(object sender, RoutedEventArgs e)
        {
            Managers.Instance.nav.Page += 1;
            Search(search.Text, Managers.Instance.nav.Page, Managers.Instance.nav.ImagePerPage);
            ScrollPannel.ScrollToTop();
        }

        private void PrevPage(object sender, RoutedEventArgs e)
        {
            Managers.Instance.nav.Page -= 1;
            if (Managers.Instance.nav.Page < 1)
                Managers.Instance.nav.Page = 1;
            Search(search.Text, Managers.Instance.nav.Page, Managers.Instance.nav.ImagePerPage);
            ScrollPannel.ScrollToTop();
        }

        private void Search(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter)
                return;

            e.Handled = true;
            Managers.Instance.nav.Page = 1;
            Search(search.Text, Managers.Instance.nav.Page, Managers.Instance.nav.ImagePerPage);
        }

        private void SearchFavoris(object sender, RoutedEventArgs e)
        {
            SearchMode();

            if (Managers.Instance.user.Connected)
            {
                Managers.Instance.nav.Page = 1;
                Pannel.Children.Clear();

                switch (Managers.Instance.service)
                {
                    case SERVICE.FLICKR:
                        foreach (Photo photo in Managers.Instance.cache.Favorite)
                        {
                            if (!Managers.Instance.user.AllowIndesirable)
                            {
                                if (!Managers.Instance.cache.IsIndesirable(photo.PhotoId))
                                    LoadImage(photo);
                            }
                            else
                                LoadImage(photo);
                        }
                        break;
                    case SERVICE.IMGUR:
                        break;
                }
            }
        }

        private void AskToken(object sender, RoutedEventArgs e)
        {
            switch (Managers.Instance.service)
            {
                case SERVICE.FLICKR:
                    Managers.Instance.flicker.AskToken();
                    break;
                case SERVICE.IMGUR:
                    Managers.Instance.imgur.AskToken();
                    break;
            }

            ValidationToken.Visibility = Visibility.Visible;
            ValidateTokenButton.Visibility = Visibility.Visible;
            AskTokenButton.Visibility = Visibility.Collapsed;
        }

        private void ValidateToken(object sender, RoutedEventArgs e)
        {
            bool ok = false;

            switch (Managers.Instance.service)
            {
                case SERVICE.FLICKR:
                    if (Managers.Instance.flicker.ValidateToken(ValidationToken.Text))
                        ok = true;
                    break;
                case SERVICE.IMGUR:
                    if (Managers.Instance.imgur.ValidateToken(ValidationToken.Text))
                        ok = true;
                    break;
            }

            if (ok)
            {
                ActionPannel.Height = new GridLength();
                ConnectionPannel.Height = new GridLength(0);
            }

            UserInfo.Text = Managers.Instance.user.UserName;
        }

        private void DisplayIndesirable(object sender, RoutedEventArgs e)
        {
            Managers.Instance.user.AllowIndesirable = !Managers.Instance.user.AllowIndesirable;
            Indesirable.Content = "Display indesirables in safe mode";
            if (Managers.Instance.user.AllowIndesirable)
                Indesirable.Content = "Hide indesirables";
        }

        private void DisplayUploaded(object sender, RoutedEventArgs e)
        {
            SearchMode();

            Pannel.Children.Clear();

            if (Managers.Instance.user.Connected)
            {
                Managers.Instance.nav.Page = 1;
                Pannel.Children.Clear();

                switch (Managers.Instance.service)
                {
                    case SERVICE.FLICKR:
                        var options = new PartialSearchOptions { Extras = PhotoSearchExtras.Description | PhotoSearchExtras.Usage, PerPage = Managers.Instance.nav.ImagePerPage, Page = Managers.Instance.nav.Page };

                        PhotoCollection photos = Managers.Instance.flicker.flickr.PhotosGetNotInSet(options);
                        foreach (Photo photo in photos)
                            LoadImage(photo);
                        break;
                    case SERVICE.IMGUR:
                        break;
                }
                
                ScrollPannel.ScrollToTop();
            }
        }

        private void UploadImage(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (Managers.Instance.service)
                {
                    case SERVICE.FLICKR:
                        Managers.Instance.flicker.flickr.OnUploadProgress += new EventHandler<FlickrNet.UploadProgressEventArgs>(OnUploadProgress);
                        Managers.Instance.flicker.Upload(Filename.Text, Title.Text, Description.Text, CheckPublic.IsChecked.Value);
                        break;
                    case SERVICE.IMGUR:
                        break;
                }
                
                System.Windows.MessageBox.Show("File " + Title.Text + " has been successfully uploaded.");
                Filename.Text = "";
                Title.Text = "";
                Description.Text = "";
                CheckPublic.IsChecked = true;
                UploadProgress.Value = 0;
            }
            catch (ArgumentException ex)
            {
                System.Windows.MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void OnUploadProgress(object sender, FlickrNet.UploadProgressEventArgs e)
        {
            UploadProgress.Value = e.ProcessPercentage;
        }

        private void SearchMode()
        {
            ScrollPannel.Visibility = Visibility.Visible;
            Pannel.Visibility = Visibility.Visible;
            Navigation.Visibility = Visibility.Visible;

            UploadForm.Visibility = Visibility.Collapsed;
        }

        private void UploadMode(object sender, RoutedEventArgs e)
        {
            Pannel.Visibility = Visibility.Collapsed;
            ScrollPannel.Visibility = Visibility.Collapsed;
            Navigation.Visibility = Visibility.Collapsed;

            UploadForm.Visibility = Visibility.Visible;
            Filename.Text = "";
            Title.Text = "";
            Description.Text = "";
            CheckPublic.IsChecked = true;
            UploadProgress.Value = 0;
        }

        private void Explore(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            var result = fileDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    var file = fileDialog.FileName;
                    Filename.Text = file;
                    Filename.ToolTip = file;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    Filename.Text = null;
                    Filename.ToolTip = null;
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Managers.Instance.cache.SaveFavorite();
            Managers.Instance.cache.SaveIndesirable();
        }

        private void AllSize_Click(object sender, RoutedEventArgs e)
        {
            if (AllSize.IsChecked == true)
            {
                SmallSearch.IsChecked = true;
                MediumSearch.IsChecked = true;
                LargeSearch.IsChecked = true;
            } else
            {
                SmallSearch.IsChecked = false;
                MediumSearch.IsChecked = false;
                LargeSearch.IsChecked = false;
            }
        }

        private void CheckSize_Click(object sender, RoutedEventArgs e)
        {
            if (SmallSearch.IsChecked == true && MediumSearch.IsChecked == true && LargeSearch.IsChecked == true)
            {
                AllSize.IsChecked = true;
            } else
            {
                AllSize.IsChecked = false;
            }
        }
    }
}
