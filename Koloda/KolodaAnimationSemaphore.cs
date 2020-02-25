using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Koloda
{
    public class KolodaAnimationSemaphore
    {
        private int _animating = 0;

        public bool IsAnimating
        {
            get
            {
                return _animating > 0;
            }
        }

        public void Increment()
        {
            _animating += 1;
        }

        public void Decrement()
        {
            _animating -= 1;
            if (_animating < 0)
            {
                _animating = 0;
            }
        }
    }
}
