﻿using System.Runtime.Serialization;

namespace MarketingBox.Integration.SimpleTrading.Bridge.Grpc.Models.Common
{
    [DataContract]
    public class Error 
    {
        [DataMember(Order = 1)]
        public ErrorType Type { get; set; }

        [DataMember(Order = 2)]
        public string Message { get; set; }

    }
}
