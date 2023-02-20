using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CityBIM.GUI
{
    public class GUI_helper
    {
        public void setRadiobutton(RadioButton rb, ListBox lb)
        {
            if (rb.IsChecked == true)
            {
                lb.SelectAll();
            }
            else
            {
                lb.UnselectAll();
            }
        }

        public void uncheckRbWhenSelected(RadioButton rb, ListBox lb)
        {
            if (lb.SelectedItems.Count < lb.Items.Count)
            {
                rb.IsChecked = false;
            }
        }
    }
}
