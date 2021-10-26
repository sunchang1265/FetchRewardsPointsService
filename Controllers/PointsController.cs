using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointsService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PointsController : ControllerBase
    {
        private readonly ILogger<PointsController> _logger;

        //holds all transaction results(positive value) by each payer
        //SortedSet will maintain the order of transactions by timestamp
        //Add() and Remove() takes log(n), Min/Max takes O(1)
        private static Dictionary<string, SortedSet<Transaction>> transactions = new();
        //holds summary of each payer's point balance
        private static Dictionary<string, int> payerPoints = new();

        public PointsController(ILogger<PointsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public int Get()
        {
            int total = 0;
            foreach (var points in payerPoints.Values)
                total += points;

            return total;
        }

        [HttpGet("BalanceDetail")]
        public Dictionary<string, int> GetBalanceDetail()
        {
            return payerPoints;
        }

        [HttpPost("Spend")]
        public IList<Transaction> Spend([FromBody] int points)
        {
            IList<Transaction> trans = new List<Transaction>();
            int remain = points; 
            while (remain != 0)
            {
                Transaction earliest = new Transaction() { Timestamp = DateTime.MaxValue };
                string selected = null;
                foreach(var payerBalance in transactions) //check each payer's earliest balance, find the earliest
                {
                    if (payerBalance.Value.Count!=0 && earliest.Timestamp.CompareTo(payerBalance.Value.Min.Timestamp) > 0)
                    {
                        selected = payerBalance.Key;
                        earliest = payerBalance.Value.Min;
                    }
                }

                if (selected == null)
                {
                    _logger.LogInformation("Insufficient balance");
                    return trans;
                }
                //handle possibly invalid transaction input makes earliest transaction has a negative value
                if (earliest.Points <= 0)
                {
                    transactions[selected].Remove(transactions[selected].Min);
                    continue;
                }

                if(remain <= earliest.Points)
                {
                    transactions[selected].Min.Points -= remain;
                    payerPoints[selected] -= remain;

                    var newTrans = new Transaction()
                    {
                        Payer = earliest.Payer,
                        Points = -remain,
                        Timestamp = earliest.Timestamp
                    };
                    trans.Add(newTrans);
                    remain = 0;
                }
                else
                {
                    remain -= transactions[selected].Min.Points;
                    payerPoints[selected] -= transactions[selected].Min.Points;

                    //Remove consumed points, operation takes log(n) for SortedSet
                    transactions[selected].Remove(transactions[selected].Min); 

                    earliest.Points *= -1;
                    trans.Add(earliest);
                }
            }
            return trans;
        }

        [HttpPost("addMultiple")]
        public string AddTransactions([FromBody] Transaction[] allTransactions)
        {
            if (allTransactions == null || allTransactions.Length == 0)
            {
                _logger.LogInformation("Invalid transaction data");
                return "Invalid transaction data";
            }

            foreach(Transaction transaction in allTransactions)
            {
                AddTransaction(transaction);                
            }

            return "Successfully Added";
        }

        [HttpPost("add")]
        public string AddTransaction([FromBody] Transaction transaction)
        {
            if (transaction == null)
            {
                _logger.LogInformation("Invalid transaction data");
                return "Invalid transaction data";
            }

            //initialize SortedSet with default comparer of the timestamp for each payer
            if (!transactions.ContainsKey(transaction.Payer))
            {
                transactions.Add(transaction.Payer,
                    new SortedSet<Transaction>(
                        Comparer<Transaction>.Create((a, b) => a.Timestamp.CompareTo(b.Timestamp))
                    ));
                transactions[transaction.Payer].Add(transaction);

                payerPoints.Add(transaction.Payer, transaction.Points);
                
            }
            else
            {
                payerPoints[transaction.Payer] += transaction.Points;
                if (transactions[transaction.Payer].Count == 0)
                {
                    transactions[transaction.Payer].Add(transaction);
                    return "Successfully Added";
                }

                // try to make sure there is only one negative value and always appear as earliest.
                // ideally after all transation is loaded, there will be no negative transactions for each payer        
                var earliest = transactions[transaction.Payer].Min;
                if (earliest.Points <= 0) //if earliest transcation of current payer has negative value
                {
                    if (transaction.Points <= 0) //merge 2 negative balance, using the later timestamp
                    {
                        transactions[transaction.Payer].Min.Timestamp = transaction.Timestamp.CompareTo(earliest.Timestamp) > 0 ? transaction.Timestamp : earliest.Timestamp;
                        transactions[transaction.Payer].Min.Points += transaction.Points;
                    }
                    else if (transaction.Timestamp.CompareTo(earliest.Timestamp) < 0) //with positive balance of current transaction, update if it has an earlier timestamp
                    {
                        transactions[transaction.Payer].Min.Points += transaction.Points;
                        transactions[transaction.Payer].Min.Timestamp = transaction.Timestamp;
                    }
                    else //simply add it to the sortedSet if it happens after earliest with positive balance
                        transactions[transaction.Payer].Add(transaction);
                }
                //earliest transaction is positve, but new transaction is negative. If new transaction happens after earliest, take the point out from earliest
                else if (transaction.Points <= 0 && transaction.Timestamp.CompareTo(earliest.Timestamp) >= 0)
                {
                    transactions[transaction.Payer].Min.Points += transaction.Points;
                }              
                else
                {
                    // rare case, when transaction is positive and have same timestamp with earliest
                    // SortedSet won't Add() if it's the same object based on Comparer
                    if(!transactions[transaction.Payer].Add(transaction) && transactions[transaction.Payer].TryGetValue(transaction, out Transaction actualValue))
                    {
                        actualValue.Points += transaction.Points;
                    }
                }
                    
            }

            return "Successfully Added";
        }
    }
}
