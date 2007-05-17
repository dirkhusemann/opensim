// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 2.0.50727.42
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace OpenGridServices.Manager {
    
    
    public partial class ConnectToGridServerDialog {
        
        private Gtk.VBox vbox2;
        
        private Gtk.Label label1;
        
        private Gtk.Entry entry1;
        
        private Gtk.Button button2;
        
        private Gtk.Button button8;
        
        protected virtual void Build() {
            Stetic.Gui.Initialize();
            // Widget OpenGridServices.Manager.ConnectToGridServerDialog
            this.Events = ((Gdk.EventMask)(256));
            this.Name = "OpenGridServices.Manager.ConnectToGridServerDialog";
            this.Title = Mono.Unix.Catalog.GetString("Connect to Grid server");
            // Internal child OpenGridServices.Manager.ConnectToGridServerDialog.VBox
            Gtk.VBox w1 = this.VBox;
            w1.Events = ((Gdk.EventMask)(256));
            w1.Name = "dialog_VBox";
            w1.BorderWidth = ((uint)(2));
            // Container child dialog_VBox.Gtk.Box+BoxChild
            this.vbox2 = new Gtk.VBox();
            this.vbox2.Name = "vbox2";
            // Container child vbox2.Gtk.Box+BoxChild
            this.label1 = new Gtk.Label();
            this.label1.Name = "label1";
            this.label1.LabelProp = Mono.Unix.Catalog.GetString("Please type in the grid server management interface URL:");
            this.label1.Wrap = true;
            this.label1.Justify = ((Gtk.Justification)(2));
            this.vbox2.Add(this.label1);
            Gtk.Box.BoxChild w2 = ((Gtk.Box.BoxChild)(this.vbox2[this.label1]));
            w2.Position = 0;
            w2.Expand = false;
            w2.Fill = false;
            // Container child vbox2.Gtk.Box+BoxChild
            this.entry1 = new Gtk.Entry();
            this.entry1.CanFocus = true;
            this.entry1.Name = "entry1";
            this.entry1.Text = Mono.Unix.Catalog.GetString("http://gridserver:8001");
            this.entry1.IsEditable = true;
            this.entry1.MaxLength = 255;
            this.entry1.InvisibleChar = '•';
            this.vbox2.Add(this.entry1);
            Gtk.Box.BoxChild w3 = ((Gtk.Box.BoxChild)(this.vbox2[this.entry1]));
            w3.Position = 1;
            w3.Expand = false;
            w3.Fill = false;
            w1.Add(this.vbox2);
            Gtk.Box.BoxChild w4 = ((Gtk.Box.BoxChild)(w1[this.vbox2]));
            w4.Position = 0;
            // Internal child OpenGridServices.Manager.ConnectToGridServerDialog.ActionArea
            Gtk.HButtonBox w5 = this.ActionArea;
            w5.Events = ((Gdk.EventMask)(256));
            w5.Name = "OpenGridServices.Manager.ConnectToGridServerDialog_ActionArea";
            w5.Spacing = 6;
            w5.BorderWidth = ((uint)(5));
            w5.LayoutStyle = ((Gtk.ButtonBoxStyle)(4));
            // Container child OpenGridServices.Manager.ConnectToGridServerDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
            this.button2 = new Gtk.Button();
            this.button2.CanDefault = true;
            this.button2.CanFocus = true;
            this.button2.Name = "button2";
            this.button2.UseUnderline = true;
            // Container child button2.Gtk.Container+ContainerChild
            Gtk.Alignment w6 = new Gtk.Alignment(0.5F, 0.5F, 0F, 0F);
            w6.Name = "GtkAlignment";
            // Container child GtkAlignment.Gtk.Container+ContainerChild
            Gtk.HBox w7 = new Gtk.HBox();
            w7.Name = "GtkHBox";
            w7.Spacing = 2;
            // Container child GtkHBox.Gtk.Container+ContainerChild
            Gtk.Image w8 = new Gtk.Image();
            w8.Name = "image37";
            w8.Pixbuf = Gtk.IconTheme.Default.LoadIcon("gtk-apply", 16, 0);
            w7.Add(w8);
            // Container child GtkHBox.Gtk.Container+ContainerChild
            Gtk.Label w10 = new Gtk.Label();
            w10.Name = "GtkLabel";
            w10.LabelProp = Mono.Unix.Catalog.GetString("Connect");
            w10.UseUnderline = true;
            w7.Add(w10);
            w6.Add(w7);
            this.button2.Add(w6);
            this.AddActionWidget(this.button2, -5);
            Gtk.ButtonBox.ButtonBoxChild w14 = ((Gtk.ButtonBox.ButtonBoxChild)(w5[this.button2]));
            w14.Expand = false;
            w14.Fill = false;
            // Container child OpenGridServices.Manager.ConnectToGridServerDialog_ActionArea.Gtk.ButtonBox+ButtonBoxChild
            this.button8 = new Gtk.Button();
            this.button8.CanDefault = true;
            this.button8.CanFocus = true;
            this.button8.Name = "button8";
            this.button8.UseUnderline = true;
            // Container child button8.Gtk.Container+ContainerChild
            Gtk.Alignment w15 = new Gtk.Alignment(0.5F, 0.5F, 0F, 0F);
            w15.Name = "GtkAlignment1";
            // Container child GtkAlignment1.Gtk.Container+ContainerChild
            Gtk.HBox w16 = new Gtk.HBox();
            w16.Name = "GtkHBox1";
            w16.Spacing = 2;
            // Container child GtkHBox1.Gtk.Container+ContainerChild
            Gtk.Image w17 = new Gtk.Image();
            w17.Name = "image38";
            w17.Pixbuf = Gtk.IconTheme.Default.LoadIcon("gtk-cancel", 16, 0);
            w16.Add(w17);
            // Container child GtkHBox1.Gtk.Container+ContainerChild
            Gtk.Label w19 = new Gtk.Label();
            w19.Name = "GtkLabel1";
            w19.LabelProp = Mono.Unix.Catalog.GetString("Cancel");
            w19.UseUnderline = true;
            w16.Add(w19);
            w15.Add(w16);
            this.button8.Add(w15);
            this.AddActionWidget(this.button8, -6);
            Gtk.ButtonBox.ButtonBoxChild w23 = ((Gtk.ButtonBox.ButtonBoxChild)(w5[this.button8]));
            w23.Position = 1;
            w23.Expand = false;
            w23.Fill = false;
            if ((this.Child != null)) {
                this.Child.ShowAll();
            }
            this.DefaultWidth = 476;
            this.DefaultHeight = 107;
            this.Show();
            this.Response += new Gtk.ResponseHandler(this.OnResponse);
        }
    }
}
