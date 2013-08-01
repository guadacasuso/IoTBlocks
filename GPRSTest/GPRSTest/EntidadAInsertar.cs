using System;
using Microsoft.SPOT;
using Microsoft.Azure.Zumo.MicroFramework.Core;

namespace GPRSTest
{
    public class EntidadAInsertar: IMobileServiceEntity
    {
        public int Id { get; set; }
        public string text { get; set; }
        public bool complete { get; set; }
    }
}
