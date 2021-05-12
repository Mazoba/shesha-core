using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class DateTimePickerModel : ComponentModelBase
    {

        public DateTimePickerModel()
        {
            ViewMode = "days";
        }

        public string DateFormat { get; set; }

        public string TimeFormat { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public bool AutoClose { get; set; }

        public string WeekStart { get; set; }

        public bool TodayHighlight { get; set; }

        public string ViewMode { get; set; }

        public string Mode { get; set; }
    }
}
