﻿using System;
using System.Linq;
using hoReverse.hoUtils.Diagrams;


// ReSharper disable once CheckNamespace
namespace hoReverse.hoUtil.EaCollection
{
    /// <summary>
    /// Handle EA collections for:
    /// - ordering diagram elements
    /// </summary>
    public class EaCollection
    {
        protected readonly EA.Repository Rep;
        public EaCollection(EA.Repository rep)
        {
            Rep = rep;
        }

        public virtual bool SortAlphabetic()
        {
            return true;
        }
    }

    public class EaCollectionDiagramObjects : EaCollection
    {
        readonly EaDiagram _eaDia;

        public EaCollectionDiagramObjects(EaDiagram eaDia) :base(eaDia.Rep)
        {
            _eaDia = eaDia;
            
        }
        /// <summary>
        /// Sort diagram objects alphabetic like:
        /// - Classifier
        /// - Port, Parameter, Pin
        /// - Packages
        /// If it sorts for Ports, it ignores other element types.
        /// </summary>
        /// <returns></returns>
        public override bool SortAlphabetic()
        {
            //EA.Element el = _rep.GetElementByID(diaObj.ElementID);
            var list = from item in _eaDia.SelObjects
                let el = Rep.GetElementByID(item.ElementID)
                select new {item.ElementID,el.Name, el.Type, item.left, item.right, item.top, item.bottom, item}
                into temp
                where temp.Type != "RequiredInterface"  && temp.Type != "ProvidedInterface"    
                orderby temp.Name
                select temp;
            var llist = list.ToList();  // run query
            // sort only Port, Pin, Parameter
            if (llist.Count(t => t.Type == "Port" || t.Type == "Pin" || t.Type == "Parameter") > 0)
            {
                llist = llist.Where(t => t.Type == "Port" || t.Type == "Pin" || t.Type == "Parameter").ToList();
            }
            // nothing to sort
            if (llist.Count() < 2) return false;

            // estimate the direction 
            bool isVertical =
                (Math.Abs(llist[0].top - llist[1].top)) >
                (Math.Abs(llist[0].left - llist[1].left)) 
                    ? true
                    : false;
            if (isVertical)
            {
                // vertical
                int topItem = llist.Max(t => t.top);
                int bottomItem = llist.Min(t => t.bottom);
                int sum = llist.Sum(t => t.top - t.bottom);
                int diff = (topItem - bottomItem - sum) / (llist.Count -1);
                int top = topItem;
                foreach (var item in llist)
                {
                    int itemDif = item.item.top - item.item.bottom;
                    item.item.top = top;
                    item.item.bottom = item.item.top - itemDif;
                    top = item.item.bottom - diff;
                    item.item.Update();
                }
                Rep.ReloadDiagram(_eaDia.Dia.DiagramID);
                return true;
            }
            else
            {
                // horizontal 
                int leftItem = llist.Min(t => t.left);
                int rightItem = llist.Max(t => t.right);
                int sum = llist.Sum(t => t.right - t.left);
                int diff = (rightItem - leftItem - sum) / (llist.Count-1);
                int left = leftItem;
                foreach (var item in llist)
                {
                    int itemDif = item.item.right - item.item.left;
                    item.item.left = left;
                    item.item.right = item.item.left + itemDif;
                    left = item.item.right + diff;
                    item.item.Update();
                }
                Rep.ReloadDiagram(_eaDia.Dia.DiagramID);
                return true;

            }
        }

    }


    
}
