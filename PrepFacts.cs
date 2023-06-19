using dbEF.Models;
using LinqKit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dbEF.ViewModel.Accounting.Remains
{
    public class PrepFacts : Base
    {
        public DateTime InvoiceDate { get; set; }
    }

    public class PrepFactsView : BaseRemains<PrepFacts>
    {
        public PrepFactsView(int PostId, DateTime StartDate, int PrepId)
       : base(PostId, StartDate, PrepId) { }

        public override List<PrepFacts> GetAll()
        {
            var predicatePrep = PredicateBuilder.New<PrepFact>(true);
            if (prepId != 0)
                predicatePrep = predicatePrep.And(y => y.Prep_id == prepId);

            List<PrepFacts> list =
            (from pf in Context.PrepFacts.Where(predicatePrep)
             join p in Context.Preparations on pf.Prep_id equals p.Id
             join i in Context.InvoiceFacts.Where(x => x.Post_id == postId && x.Accepted==1 && x.InvoiceDate==startDate) 
                on pf.InvoiceFact_id equals i.Id

             orderby i.InvoiceDate descending

             select new PrepFacts
             {
                 InvoiceDate = (DateTime)i.InvoiceDate,                 
                 Prep_id = pf.Prep_id,
                 Qnty = (decimal)pf.Qnty,
                 IsPack = (int)pf.IsPack,
                 IsUnit = (int)pf.IsUnit,

                 CellQnty = (decimal)p.CellQnty,
                 PackQnty = (int)p.PackQnty
             }
               ).ToList();
            
            return list;
        }
    }
}
