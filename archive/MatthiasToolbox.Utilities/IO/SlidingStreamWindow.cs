using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace MatthiasToolbox.Utilities.IO
{
    /// <summary>
    /// The SlidingStreamWindow class provides a windowed view on 
    /// a binary data stream in form of a byte array. The window
    /// size is fixed and moves in overlapping steps of a single
    /// byte. It is also possible to skip a given number of bytes.
    /// Implements IEnumerable&lt;byte[]&gt;
    /// </summary>
    /// <remarks>FINAL</remarks>
    public class SlidingStreamWindow : IEnumerable<byte[]>
    {
        #region cvar
        
        private Stream inputStream;     // the stream over which the window slides

        private int currentIndex;       // current reading position in the inputBuffer 
        private int currentSize;        // current sie of the input buffer

        private byte[] readBuffer;      // buffer for reading from the stream
        private byte[] inputBuffer;     // buffer holding data from the stream
        private byte[] outputBuffer;    // buffer for requested window
        private byte[] tempBuffer;      // temporary buffer

        private int windowSize;         // the window size
        private int blocksToBuffer;     // number of window sized blocks to hold in a buffer
                                        // must be greater or equal 2

        private bool endReached;        // flags if the stream was read to the end
        private bool finished;          // flags if the last window was returned
        
        #endregion
        #region prop

        /// <summary>
        /// Indicates if the last window was processed.
        /// </summary>
        public bool Finished
        {
            get { return finished; }
        }

        /// <summary>
        /// Get or set the current stream position.
        /// </summary>
        public int CurrentIndex 
        {
            get { return currentIndex; }
            set { currentIndex = value; }
        }

        #endregion
        #region ctor

        /// <summary>
        /// Creates a new SlidingStreamWindow over the specified stream.
        /// This constructor sets the window size to 1024 and the buffer size to 4 blocks.
        /// </summary>
        /// <param name="inputStream">
        /// The stream over which to slide. If the stream is null or 
        /// <code>inputStream.CanRead</code> is false an 
        /// <code>ArgumentException</code> will be thrown.
        /// </param>
        public SlidingStreamWindow(Stream inputStream)
            : this(inputStream, 1024, 4)
        { }

        /// <summary>
        /// Creates a new SlidingStreamWindow
        /// </summary>
        /// <param name="inputStream">
        /// The stream over which to slide. If the stream is null or 
        /// <code>inputStream.CanRead</code> is false an 
        /// <code>ArgumentException</code> will be thrown.
        /// </param>
        /// <param name="windowSize">
        /// The size of the sliding window.
        /// </param>
        /// <param name="blocksToBuffer">
        /// The number of window sized blocks to hold in a buffer.
        /// The given value must be greater or equal 2, otherwise an 
        /// <code>ArgumentOutOfRangeException</code> will be thrown.
        /// </param>
        public SlidingStreamWindow(Stream inputStream, int windowSize, int blocksToBuffer)
        {
            // check the input stream
            if (inputStream == null || !inputStream.CanRead)
            {
                throw new ArgumentException("Stream is not readable.", "inputStream");
            }
            // check constraint
            if (blocksToBuffer < 2)
            {
                throw new ArgumentOutOfRangeException("blocksToBuffer", "At least 2 blocks need to be buffered.");
            }
            // initialize classvars
            this.inputStream = inputStream;
            this.windowSize = windowSize;
            this.blocksToBuffer = blocksToBuffer;
            this.currentIndex = 0;
            this.currentSize = 0;
            this.readBuffer = new byte[windowSize * blocksToBuffer];
            this.inputBuffer = new byte[windowSize * (blocksToBuffer + 1)];
            this.outputBuffer = new byte[windowSize];
            this.tempBuffer = new byte[windowSize];
            this.finished = false;
        } // void

        #endregion
        #region impl

        #region Main

        /// <summary>
        /// Retrieve one overlapping sliding window after the other, advancing bytewise.
        /// </summary>
        /// <returns>
        /// Window sized array of bytes from the inputstream
        /// starting at <code>CurrentIndex</code>, which is incremented by one after each call.
        /// </returns>
        private byte[] GetNextWindow()
        {
            // check if the current inputBuffer contains enough data
            if (currentIndex + windowSize < currentSize)
            {
                // copy requested window starting at currentTndex to outputBuffer
                Array.Copy(inputBuffer, currentIndex, outputBuffer, 0, windowSize);
                // increment currentIndex
                currentIndex++;
                // return window
                return outputBuffer;
            }
            else
            {
                // check if stream still contains data
                if (endReached)
                {
                    // if the full file is smaller than the block size:
                    //if (currentSize <= windowSize && currentSize > 0)
                    if (currentSize > 0)
                    {
                        // make accordingly smaller result buffer
                        byte[] result = new byte[currentSize - currentIndex];
                        // changed by Matthias Gruber from Array.Copy(readBuffer, 0, result, 0, currentSize); to
                        Array.Copy(inputBuffer, currentIndex, result, 0, currentSize - currentIndex);
                        currentSize = 0;
                        finished = true;
                        return result;
                    } // if
                    // no data left to return
                    finished = true;
                    return null;
                } // if
                // else
                // update buffer first
                UpdateBuffer();
                // try again by recursion
                return GetNextWindow();
            } // if
        } // byte[]

        /// <summary>
        /// Configures the GetNextWindow function to resume with the next non-overlapping window.
        /// </summary>
        public void SkipRestOfWindow()
        {
            currentIndex += windowSize - 1;
        } // void

        /// <summary>
        /// Configures the GetNextWindow function to resume processing at n bytes further downstream.
        /// </summary>
        public void Skip(int n)
        {
            currentIndex += n;
        } // void

        /// <summary>
        /// Updates the internal read buffer.
        /// </summary>
        private void UpdateBuffer()
        {
            // indicate data which was already read
            int oldDataLength = 0;

            // if input buffer already contains data
            if (currentSize > 0)
            {
                oldDataLength = currentSize - currentIndex;
                // save earlier read data in temporary buffer
                Array.Copy(inputBuffer, currentIndex, tempBuffer, 0, oldDataLength);
            } // if

            // read blocksToBuffer times the windowSize from the inputStram
            int readSize = inputStream.Read(readBuffer, 0, windowSize * blocksToBuffer);

            // build the new buffer:
            // start with older data
            Array.Copy(tempBuffer, 0, inputBuffer, 0, oldDataLength);
            // append data which was read above
            Array.Copy(readBuffer, 0, inputBuffer, oldDataLength, readSize);

            // adjust currentSize variable
            currentSize = readSize + oldDataLength;

            // flag if the end of the stream was reached
            if (currentSize < windowSize * blocksToBuffer)
            {
                endReached = true;
            } // if

            // reset currentIndex for reading in inputBuffer
            currentIndex = 0;
        } // void

        #endregion
        #region IEnumerable

        ///<summary>
        ///Returns an enumerator which iterates through the collection of windows.
        ///</summary>
        ///<returns>
        ///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> which can be used to iterate through the collection of windows.
        ///</returns>
        public IEnumerator<byte[]> GetEnumerator()
        {
            finished = false;
            byte[] result; // holds enumeration elements
            while ((result = GetNextWindow()) != null)
            {
                yield return result; // return bytewise subsequent windows
            } // while
        } // IEnumerator<byte[]>

        ///<summary>
        ///Returns an enumerator which iterates through the collection of windows.
        ///</summary>
        ///<returns>
        ///An <see cref="T:System.Collections.IEnumerator"></see> object which can be used to iterate through the collection of windows.
        ///</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        } // IEnumerator

        #endregion

        #endregion
    } // class
} // namespace