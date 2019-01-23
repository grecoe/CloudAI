using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SubDataUpload.DataFormat
{
    class VirtualMachineData
    {
        public int Stopped { get; set; }
        public int Running { get; set; }
        public int Deallocated { get; set; }
        public int Total { get; set; }
    }

    class ResourceGroupData
    {
        public int Total { get; set; }
        public int Older60Days { get; set; }
        public int Specials { get; set; }
        public Dictionary<String, int> Regions { get; set; }

        public ResourceGroupData()
        {
            this.Regions = new Dictionary<String, int>();
        }
    }



    class SubscriptionData
    {
        [JsonIgnore]
        public String SubscriptionId { get; set; }
        [JsonProperty(PropertyName = "ResourceGroups")]
        public ResourceGroupData GroupData { get; set; }
        [JsonProperty(PropertyName = "VirtualMachines")]
        public VirtualMachineData VirtualMachines { get; set; }
        [JsonProperty(PropertyName = "ResourceUsage")]
        public Dictionary<String,int> Resources { get; set; }

        public SubscriptionData()
        {
            this.GroupData = new ResourceGroupData();
            this.VirtualMachines = new VirtualMachineData();
            this.Resources = new Dictionary<string, int>();
        }
    }
}
