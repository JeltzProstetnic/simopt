///////////////////////////////////////////////////////////////////////////////////////
//
//        Project: BlueLogic.SDelta
//    Description: 
//         Status: RELEASE
//
// ------------------------------------------------------------------------------------
//    Copyright 2007 by Bluelogic Software Solutions.
//    see product licence ( creative commons attribution 3.0 )
// ------------------------------------------------------------------------------------
//
//    History:
//
//    Dienstag, 08. Mai 2007 Matthias Gruber original version
//    Mittwoch, 09. Mai 2007 Matthias Gruber finished implementation, comments, unit tests
//    Mittwoch, 09. Mai 2007 FINAL
//
//
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;

namespace MatthiasToolbox.Cryptography.Utilities
{
    /// <summary>
    /// A wrapper to append data to any stream
    /// </summary>
    public class PostfixStream : Stream
    {
        #region classvar
        
        private Stream originalStream;
        private byte[] postFix;
        private int postFixIndex;
        
        #endregion
        #region Stream Members
        
        ///<summary>
        ///When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        ///</summary>
        ///
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
        public override void Flush()
        {
            originalStream.Flush();
        }

        ///<summary>
        ///When overridden in a derived class, sets the position within the current stream.
        ///</summary>
        ///
        ///<returns>
        ///The new position within the current stream.
        ///</returns>
        ///
        ///<param name="offset">A byte offset relative to the origin parameter. </param>
        ///<param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"></see> indicating the reference point used to obtain the new position. </param>
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        ///<exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.NotSupportedException.#ctor(System.String)")]
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("A postfixstream cannot seek even if the underlying stream can.");
        }

        ///<summary>
        ///When overridden in a derived class, sets the length of the current stream.
        ///</summary>
        ///
        ///<param name="value">The desired length of the current stream in bytes. </param>
        ///<exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>2</filterpriority>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.NotSupportedException.#ctor(System.String)")]
        public override void SetLength(long value)
        {
            throw new NotSupportedException("a postfixstream cannot be changed in length");
        }

        ///<summary>
        ///When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        ///</summary>
        ///
        ///<returns>
        ///The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        ///</returns>
        ///
        ///<param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream. </param>
        ///<param name="count">The maximum number of bytes to be read from the current stream. </param>
        ///<param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source. </param>
        ///<exception cref="T:System.ArgumentException">The sum of offset and count is larger than the buffer length. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        ///<exception cref="T:System.NotSupportedException">The stream does not support reading. </exception>
        ///<exception cref="T:System.ArgumentNullException">buffer is null. </exception>
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        ///<exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception><filterpriority>1</filterpriority>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int pos = originalStream.Read(buffer, offset, count);
            while(pos < count && postFixIndex <= postFix.GetUpperBound(0))
            {
                buffer[pos] = postFix[postFixIndex];
                postFixIndex += 1;
                pos += 1;
            }
            return pos;
        }

        ///<summary>
        ///When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        ///</summary>
        ///
        ///<param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream. </param>
        ///<param name="count">The number of bytes to be written to the current stream. </param>
        ///<param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream. </param>
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        ///<exception cref="T:System.NotSupportedException">The stream does not support writing. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        ///<exception cref="T:System.ArgumentNullException">buffer is null. </exception>
        ///<exception cref="T:System.ArgumentException">The sum of offset and count is greater than the buffer length. </exception>
        ///<exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception><filterpriority>1</filterpriority>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.NotSupportedException.#ctor(System.String)")]
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("A postfix stream cannot be witten to!");
        }

        ///<summary>
        ///When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        ///</summary>
        ///
        ///<returns>
        ///true if the stream supports reading; otherwise, false.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        public override bool CanRead
        {
            get { return originalStream.CanRead; }
        }

        ///<summary>
        ///When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        ///</summary>
        ///
        ///<returns>
        ///true if the stream supports seeking; otherwise, false.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        public override bool CanSeek
        {
            get { return false; }
        }

        ///<summary>
        ///When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        ///</summary>
        ///
        ///<returns>
        ///true if the stream supports writing; otherwise, false.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        public override bool CanWrite
        {
            get { return false; }
        }

        ///<summary>
        ///When overridden in a derived class, gets the length in bytes of the stream.
        ///</summary>
        ///
        ///<returns>
        ///A long value representing the length of the stream in bytes.
        ///</returns>
        ///
        ///<exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override long Length
        {
            get { return originalStream.Length + postFix.Length; }
        }

        ///<summary>
        ///When overridden in a derived class, gets or sets the position within the current stream.
        ///</summary>
        ///
        ///<returns>
        ///The current position within the stream.
        ///</returns>
        ///
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        ///<exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.NotSupportedException.#ctor(System.String)")]
        public override long Position
        {
            get { return originalStream.Position; }
            set { throw new NotSupportedException("A postfixstream cannot seek even if the underlying stream can."); }
        }

        #endregion
        #region constructor
        
        /// <summary>
        /// constructor for a stream with appended data
        /// </summary>
        /// <param name="original">the original data stream</param>
        /// <param name="postFix">the postfix to append</param>
        public PostfixStream(Stream original, byte[] postFix)
        {
            originalStream = original;
            this.postFix = postFix;
            postFixIndex = 0;
        }

        #endregion
    } // class
}  // namespace 
