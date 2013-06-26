using System;
using System.ComponentModel;
using System.Threading;

namespace PD.Core
{
    /// <summary>
    /// Data model for directory information
    /// </summary>
    public class DirectoryModel : INotifyPropertyChanged
    {
        private string path;
        private long size;
        public CancellationTokenSource TokenSource { get; set; }

        /// <summary>
        /// Id of directory
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Directory full path
        /// </summary>
        public string Path
        {
            get { return path; }
            set
            {
                path = value;
                NotifyPropertyChanged("Path");
            }
        }

        /// <summary>
        /// Size of directory in bytes
        /// </summary>
        public long Size
        {
            get { return size; }
            set
            {
                size = value;
                NotifyPropertyChanged("Size");
                NotifyPropertyChanged("SizeMB");
                NotifyPropertyChanged("SizeGB");
            }
        }

        /// <summary>
        /// Size of directory in megabytes
        /// </summary>
        public double SizeMB
        {
            get { return Size == 0 ? 0 : Size.BytesToMB(); }
        }

        /// <summary>
        /// Size of directory in gigabytes
        /// </summary>
        public double SizeGB
        {
            get { return Size == 0 ? 0 : Size.BytesToGB(); }
        }


        public DirectoryModel(int id, string path, long size)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException("path not supplied");

            if (size < 0)
                throw new ArgumentException("Size must be greater than zero.");

            Path = path;
            Size = size;
            Id = id;
        }

        public void Reset()
        {
            this.Path = "<Select Directory>";
            this.Size = 0;
        }

        // To support auto-updates on UI
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
