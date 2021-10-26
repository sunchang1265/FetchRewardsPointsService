using System;

namespace PointsService
{
    public class Transaction
    {
        public DateTime Timestamp { get; set; }

        public int Points { get; set; }

        public string Payer { get; set; }
    }
}
