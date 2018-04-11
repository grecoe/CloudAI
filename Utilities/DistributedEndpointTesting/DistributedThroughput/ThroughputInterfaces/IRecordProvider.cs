//
// Copyright  Microsoft Corporation ("Microsoft").
//
// Microsoft grants you the right to use this software in accordance with your subscription agreement, if any, to use software 
// provided for use with Microsoft Azure ("Subscription Agreement").  All software is licensed, not sold.  
// 
// If you do not have a Subscription Agreement, or at your option if you so choose, Microsoft grants you a nonexclusive, perpetual, 
// royalty-free right to use and modify this software solely for your internal business purposes in connection with Microsoft Azure 
// and other Microsoft products, including but not limited to, Microsoft R Open, Microsoft R Server, and Microsoft SQL Server.  
// 
// Unless otherwise stated in your Subscription Agreement, the following applies.  THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT 
// WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL MICROSOFT OR ITS LICENSORS BE LIABLE 
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THE SAMPLE CODE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
//
using System;
using System.Collections.Generic;
using ThroughputInterfaces.Configuration;

namespace ThroughputInterfaces
{

    public interface IRecordProvider
    {
        /// <summary>
        /// Endpoint that is the target of this test
        /// </summary>
        String EndpointUrl { get; }
        /// <summary>
        /// Key for the endpoint target
        /// </summary>
        String EndpointKey { get; }

        /// <summary>
        /// Loads the number of records identified in RecordProviderConfiguration.RecordCount and returns them as an 
        /// enumeration of key value pairs.
        /// </summary>
        /// <param name="configuration">Confiuguraiton indicating how/where to load records from.</param>
        /// <param name="onStatus">Optional delegate to report status to calling application</param>
        /// <returns>Enumeration of key value pairs
        /// key = unique record identifier
        /// value = Request payload. 
        ///     If this is a string it is passed raw as the payload to the endpoint
        ///     If this is an object it is deserialized with JSON.net to create the payload
        ///</returns>
        IDictionary<int, object> LoadRecords(RecordProviderConfiguration configuration, OnStatusUpdate onStatus = null);
    }
}
