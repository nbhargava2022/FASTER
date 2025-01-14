// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Net.Sockets;
using FASTER.common;

namespace FASTER.server
{
    /// <summary>
    /// Abstract base class for server session provider
    /// </summary>
    public abstract class ServerSessionBase : IServerSession
    {
        /// <summary>
        /// Socket
        /// </summary>
        protected readonly Socket socket;

        /// <summary>
        /// Max size settings
        /// </summary>
        protected readonly MaxSizeSettings maxSizeSettings;

        /// <summary>
        /// Response object
        /// </summary>
        protected SeaaBuffer responseObject;

        /// <summary>
        /// Bytes read
        /// </summary>
        protected int bytesRead;

        /// <summary>
        /// Message manager
        /// </summary>
        protected readonly NetworkSender messageManager;

        private readonly int serverBufferSize;


        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="maxSizeSettings"></param>
        public ServerSessionBase(Socket socket, MaxSizeSettings maxSizeSettings)
        {
            this.socket = socket;
            this.maxSizeSettings = maxSizeSettings;
            serverBufferSize = BufferSizeUtils.ServerBufferSize(maxSizeSettings);
            messageManager = new NetworkSender(serverBufferSize);
            bytesRead = 0;
        }

        /// <inheritdoc />
        public abstract int TryConsumeMessages(byte[] buf);

        /// <inheritdoc />
        public void AddBytesRead(int bytesRead) => this.bytesRead += bytesRead;

        /// <summary>
        /// Get response object
        /// </summary>
        protected void GetResponseObject() { if (responseObject == null) responseObject = messageManager.GetReusableSeaaBuffer(); }

        /// <summary>
        /// Send response
        /// </summary>
        /// <param name="size"></param>
        protected void SendResponse(int size)
        {
            var _r = responseObject;
            responseObject = null;
            try
            {
                messageManager.Send(socket, _r, 0, size);
            }
            catch
            {
                messageManager.Return(_r);
            }
        }

        /// <summary>
        /// Send response
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        protected void SendResponse(int offset, int size)
        {
            var _r = responseObject;
            responseObject = null;
            try
            {
                messageManager.Send(socket, _r, offset, size);
            }
            catch
            {
                messageManager.Return(_r);
            }
        }

        /// <summary>
        /// Publish an update to a key to all the subscribers of the key
        /// </summary>
        /// <param name="keyPtr"></param>
        /// <param name="keyLength"></param>
        /// <param name="valPtr"></param>
        /// <param name="valLength"></param>
        /// <param name="inputPtr"></param>
        /// <param name="sid"></param>
        public abstract unsafe void Publish(ref byte* keyPtr, int keyLength, ref byte* valPtr, int valLength, ref byte* inputPtr, int sid);

        /// <summary>
        /// Publish an update to a key to all the (prefix) subscribers of the key
        /// </summary>
        /// <param name="prefixPtr"></param>
        /// <param name="prefixLength"></param>
        /// <param name="keyPtr"></param>
        /// <param name="keyLength"></param>
        /// <param name="valPtr"></param>
        /// <param name="valLength"></param>
        /// <param name="inputPtr"></param>
        /// <param name="sid"></param>
        public abstract unsafe void PrefixPublish(byte* prefixPtr, int prefixLength, ref byte* keyPtr, int keyLength, ref byte* valPtr, int valLength, ref byte* inputPtr, int sid);

        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
            socket.Dispose();
            var _r = responseObject;
            if (_r != null)
                messageManager.Return(_r);
            messageManager.Dispose();
        }

        /// <summary>
        /// Wait for ongoing outgoing calls to complete
        /// </summary>
        public virtual void CompleteSends()
        {
            var _r = responseObject;
            if (_r != null)
                messageManager.Return(_r);
            messageManager.Dispose();
        }
    }
}
