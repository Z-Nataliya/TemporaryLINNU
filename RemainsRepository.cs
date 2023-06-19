using System;
using System.Collections.Generic;
using System.Linq;

namespace dbEF.ViewModel.Accounting.Remains
{
    public class RemainsTotal : Base
    {
        public string Name { get; set; }
        public string Unit_name { get; set; }
        public string Pack_name { get; set; }
        public string PrepType_name { get; set; }
        public string PrepCath_name { get; set; }
        public int PrepCathId { get; set; }

        public string Cell { get => $"{FormatConverter.ViewDecimalValue(CellQnty)} {Unit_name}"; }
        public string Pack { get => $"{Convert.ToString(PackQnty)} {Pack_name}"; }

        public decimal? QntyIncomes { get; set; }
        public decimal? QntyDemands { get; set; }
        public decimal? QntyFacts { get; set; }
        public decimal? QntyDiscards { get; set; }
        public decimal? QntyDiscardPosts { get; set; }
        public decimal? QntyPrepSold { get; set; }

        public decimal QntyPackRest { get => (decimal)QntyIncomes + (decimal)QntyFacts - (decimal)QntyDemands; }
        public decimal QntyPackRestPost { get => 
                (decimal)QntyFacts + (decimal)QntyDemands-(decimal)QntyDiscards-(decimal)QntyDiscardPosts; }
        public decimal QntyPackRestCosmet { get => 
                (decimal)QntyIncomes + (decimal)QntyFacts - (decimal)QntyDiscards - (decimal)QntyPrepSold; }

    }
    public class RemainsRepository: BaseRemains<RemainsTotal>
    {
        public RemainsViewModel(int PostId, DateTime StartDate, int PrepId)
       : base(PostId, StartDate, PrepId) { }        

        private List<T> GroupByPrep<T>(List<T> list)
            where T : Base, new()
        {
            return list
                .GroupBy(g => new { g.Prep_id, g.PackQnty, g.CellQnty })
                .Select(g => new T
                {
                    Prep_id = g.Key.Prep_id,
                    PackQnty = g.Key.PackQnty,
                    CellQnty = g.Key.CellQnty,
                    QntyPacksSum = g.Sum(s => s.QntyPacks)
                }).ToList();
        }

        public List<RemainsTotal> GetAllHead()
        {
            using var repoPrepFacts = new PrepFactsView(postId, startDate, prepId);
            List<PrepFacts> prepFacts = GroupByPrep(repoPrepFacts.GetAll());

            using var repoIncomes = new IncomesView(postId, startDate, prepId);
            List<Incomes> incomes = GroupByPrep(repoIncomes.GetAll());

            using var repoDemands = new DemandsView(postId, startDate, prepId);
            List<Demands> demands = GroupByPrep(repoDemands.GetAll());
                
            List<RemainsTotal> list =
                (from f in prepFacts 

                 join p in Context.Preparations on f.Prep_id equals p.Id
                 join pt in Context.PrepTypes on p.PrepType_id equals pt.Id
                 join pack in Context.Packs on p.Pack_id equals pack.Id
                 join unit in Context.Units on p.Unit_id equals unit.Id
                 join pc in Context.PrepCaths on p.PrepCath_id equals pc.Id

                 join d in demands on f.Prep_id equals d.Prep_id into g_d
                 from subd in g_d.DefaultIfEmpty()

                 join inc in incomes on f.Prep_id equals inc.Prep_id into g_inc
                 from subinc in g_inc.DefaultIfEmpty()

                 orderby p.Name

                 select new RemainsTotal
                 {
                     Id = p.Id,
                     Name = p.Name,
                     PrepType_name = pt.Name,
                     CellQnty = (decimal)p.CellQnty,
                     Unit_name = unit.Name,
                     PackQnty = (int)p.PackQnty,
                     Pack_name = pack.Name,
                     PrepCath_name = pc.Name,
                    
                     QntyDemands = (subd != null) ? subd.QntyPacksSum : 0,
                     QntyFacts = (f != null) ? f.QntyPacksSum : 0,
                     QntyIncomes = (subinc != null) ? subinc.QntyPacksSum : 0,
                 }

                ).ToList();            

            return list;
        }

        public List<RemainsTotal> GetAllPosts()
        {
            using var repoPrepFacts = new PrepFactsView(postId, startDate, prepId);
            List<PrepFacts> prepFacts = GroupByPrep(repoPrepFacts.GetAll());

            using var repoDemands = new DemandsView(postId, startDate, prepId);
            List<Demands> demands = GroupByPrep(repoDemands.GetAll());

            using var repoDiscards = new DiscardProcesView(postId, startDate, prepId);
            List<DiscardProces> discards = GroupByPrep(repoDiscards.GetAll());

            using var repoDiscardPosts = new DiscardPostView(postId, startDate, prepId);
            List<DiscardPosts> discardPost = GroupByPrep(repoDiscardPosts.GetAll());
                        
            List<RemainsTotal> list =
                (from f in prepFacts

                 join p in Context.Preparations on f.Prep_id equals p.Id
                 join pt in Context.PrepTypes on p.PrepType_id equals pt.Id
                 join pack in Context.Packs on p.Pack_id equals pack.Id
                 join unit in Context.Units on p.Unit_id equals unit.Id
                 join pc in Context.PrepCaths on p.PrepCath_id equals pc.Id

                 join d in demands on f.Prep_id equals d.Prep_id into g_d
                 from subd in g_d.DefaultIfEmpty()

                 join disc in discards on f.Prep_id equals disc.Prep_id into g_disc
                 from subdisc in g_disc.DefaultIfEmpty()

                 join discP in discardPost on f.Prep_id equals discP.Prep_id into g_discP
                 from subdiscP in g_discP.DefaultIfEmpty()

                 select new RemainsTotal
                 {
                     Id = f.Prep_id,
                     Name = p.Name,
                     PrepType_name = pt.Name,
                     CellQnty = (decimal)p.CellQnty,
                     Unit_name = unit.Name,
                     PackQnty = (int)p.PackQnty,
                     Pack_name = pack.Name,
                     PrepCath_name = pc.Name,

                     QntyDemands = (subd != null) ? subd.QntyPacksSum : 0,
                     QntyFacts = (f != null) ? f.QntyPacksSum : 0,
                     QntyDiscardPosts = (subdiscP != null) ? subdiscP.QntyPacksSum : 0,
                     QntyDiscards = (subdisc != null) ? subdisc.QntyPacksSum : 0
                 }

                ).ToList();
            return list;
        }

        public List<RemainsTotal> GetAllCosmetology()
        {
            using var repoPrepFacts = new PrepFactsView(postId, startDate, prepId);
            List<PrepFacts> prepFacts = GroupByPrep(repoPrepFacts.GetAll());

            using var repoIncomes = new IncomesView(postId, startDate, prepId);
            List<Incomes> incomes = GroupByPrep(repoIncomes.GetAllForCosmetology());

            using var repoDiscards = new DiscardProcesView(postId, startDate, prepId);
            List<DiscardProces> discards = GroupByPrep(repoDiscards.GetAll());

            using var repoPrepSold = new PrepSoldsView(postId, startDate, prepId);
            List<PrepSolds> prepSolds = GroupByPrep(repoPrepSold.GetAll());

            List<RemainsTotal> list =
                (from f in prepFacts

                 join p in Context.Preparations on f.Prep_id equals p.Id
                 join pt in Context.PrepTypes on p.PrepType_id equals pt.Id
                 join pack in Context.Packs on p.Pack_id equals pack.Id
                 join unit in Context.Units on p.Unit_id equals unit.Id
                 join pc in Context.PrepCaths on p.PrepCath_id equals pc.Id

                 join inc in incomes on f.Prep_id equals inc.Prep_id into g_inc
                 from subinc in g_inc.DefaultIfEmpty()

                 join disc in discards on f.Prep_id equals disc.Prep_id into g_disc
                 from subdisc in g_disc.DefaultIfEmpty()

                 join prepsold in prepSolds on f.Prep_id equals prepsold.Prep_id into g_prepsold
                 from subprepsold in g_prepsold.DefaultIfEmpty()

                 select new RemainsTotal
                 {
                     Id = f.Prep_id,
                     Name = p.Name,
                     PrepType_name = pt.Name,
                     CellQnty = (decimal)p.CellQnty,
                     Unit_name = unit.Name,
                     PackQnty = (int)p.PackQnty,
                     Pack_name = pack.Name,
                     PrepCath_name = pc.Name,
                     PrepCathId=p.PrepCath_id,

                     QntyFacts = (f != null) ? f.QntyPacksSum : 0,
                     QntyIncomes = (subinc != null) ? subinc.QntyPacksSum : 0,
                     QntyDiscards = (subdisc != null) ? subdisc.QntyPacksSum : 0,
                     QntyPrepSold = (subprepsold != null) ? subprepsold.QntyPacksSum : 0
                 }

                ).ToList();
            return list;
        }
    }
}
