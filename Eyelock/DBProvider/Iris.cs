//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Eyelock.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class Iris
    {
        public System.Guid UID { get; set; }
        public System.Guid UserID { get; set; }
        public byte[] Image_LL { get; set; }
        public byte[] Image_Display_LL { get; set; }
        public byte[] Image_RL { get; set; }
        public byte[] Image_Display_RL { get; set; }
        public byte[] Image_LR { get; set; }
        public byte[] Image_Display_LR { get; set; }
        public byte[] Image_RR { get; set; }
        public byte[] Image_Display_RR { get; set; }
        public Nullable<int> Type { get; set; }
        public short StorageType { get; set; }
    
        public virtual User User { get; set; }
        public virtual IrisLite IrisLite { get; set; }
    }
}
