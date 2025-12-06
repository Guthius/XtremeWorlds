using Core.Objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
    public class Stream
    {
        public Stream()
        {
            _isLoaded = false;
        }

        bool _isLoaded;

        public bool IsLoaded
        {
            get { return _isLoaded; }
            set { _isLoaded = value; }
        }
    }
}
