using dbEF.Models;
using dbEF.Models.Base.Abstracts;
using LinqKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;


namespace dbEF.ViewModel.Accounting.Discards
{
    public class DiscardPricesViewModel : PreparationDiscard
    {
        public decimal? Price { get; set; }
        public string Currency_name { get; set; }
        public int? Currency_id { get; set; }
        public decimal? Nds { get; set; }
        
        public decimal PriceWithNds
        {
            get => Math.Round(Convert.ToDecimal(Price) + Convert.ToDecimal(Price) * Convert.ToDecimal(Nds) / 100, 2);
        }
        [NotMapped]
        public decimal PricePerUnit
        {
            get
            {
                if (IsPack == 1)
                    return Math.Round(Convert.ToDecimal((PriceWithNds / PackQnty) * Qnty), 2);
                else if (IsUnit == 1)
                    return Math.Round(Convert.ToDecimal((PriceWithNds / PackQnty / CellQnty) * Qnty), 2);
                else return Math.Round(Convert.ToDecimal(PriceWithNds * Qnty), 2);
            }
        }      
    }

    public class DiscardViewPricesRepository<T> : BaseRepoView<T> 
        where T : DiscardPricesViewModel, new()              
    {             
        protected List<T> GetAllInternal<K>(DateTime dateTime, 
            List<K> discardList, out string exMessage)
            where K:DiscardCommon, new()
        {
            exMessage = "";
            
            var predicate = PredicateBuilder.New<Invoice>(true);
            predicate = predicate.And(x => x.InvoiceDate <= dateTime);

            List<T> list = new();

            try
            {
                list =
                    (from sd in discardList
                     join pv in Context.Preparations on sd.Prep_id equals pv.Id
                     join pt in Context.PrepTypes on pv.PrepType_id equals pt.Id
                     join pack in Context.Packs on pv.Pack_id equals pack.Id
                     join unit in Context.Units on pv.Unit_id equals unit.Id
                     join v1 in
                        (from inc in Context.Incomes
                         join v2 in
                               (from inc in Context.Incomes
                                join inv in Context.Invoices.Where(predicate)
                                  on inc.Invoice_id equals inv.Id into gp
                                from subinv in gp.DefaultIfEmpty()

                                group subinv.InvoiceDate by new { inc.Prep_id } into gp
                                select new
                                {
                                    gp.Key.Prep_id,
                                    MaxInvoiceDate = gp.Max()
                                }
                                ) on inc.Prep_id equals v2.Prep_id into gp
                         from subv2 in gp.DefaultIfEmpty()
                         join inv in Context.Invoices on inc.Invoice_id equals inv.Id into ginv
                         from subinv in ginv.DefaultIfEmpty()

                         where subinv.InvoiceDate == subv2.MaxInvoiceDate

                         select new
                         {
                             subv2.Prep_id,
                             inc.Price,
                             inc.Currency_id,
                             subinv.Nds
                         }
                          ) on sd.Prep_id equals v1.Prep_id into gp
                     from subv1 in gp.DefaultIfEmpty()

                     join cur in Context.Currencies on subv1.Currency_id equals cur.Id into gcur
                     from subcur in gcur.DefaultIfEmpty()

                     orderby pv.Name

                     select new T
                     {
                         Id = sd.Id,
                         Prep_id = sd.Prep_id,
                         Qnty = sd.Qnty,
                         IsPack = sd.IsPack,
                         IsUnit = sd.IsUnit,
                         Prep_name = pv.Name,
                         PackQnty = pv.PackQnty,
                         CellQnty = pv.CellQnty,
                         Unit_name = unit.Name,
                         PrepType_name = pt.Name,
                         Pack_name = pack.Name,
                         Price = subv1.Price,
                         Currency_id = subv1.Currency_id,
                         Nds = subv1.Nds,
                         Currency_name = subcur.Name
                     }

                    ).ToList();
            }
            catch (NullReferenceException)
            {
                exMessage = "Один або кілька товарів не можуть бути використані. Немає приходної накладної.";
            }            
            return list;
        }

        public virtual List<T> GetAll(int proc_id, DateTime dateTime, out string exMessage)
        {
            exMessage = "";
            return new List<T>();
        }
    }
}

