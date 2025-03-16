//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Drawing;
//using System.Runtime.InteropServices;
//using System.Windows.Forms;
//using ZYing.Interface;
//using static System.Net.Mime.MediaTypeNames;
//using static System.Drawing.Image;
//using System.Threading;
//using Enumer;
//using ZYing.Web;
//using System.IO;

//namespace ZYing.UI;

//[DefaultProperty("Items")]
//[ClassInterface(ClassInterfaceType.AutoDispatch)]
//[DefaultEvent("OnSelectedChanged")]
//[ComVisible(true)]
//public class EPanelEx : EPanelBase
//{
//	private static ContextMenuStrip _停靠;

//	private const int _dropWidth = 11;

//	private const int XHeight = 11;

//	private EFormOwner _dockForm;

//	private Control _dockCtrl;

//	private EControl _hover;

//	private EControl _xc;

//	private bool _isdown;

//	private bool _isfresh;

//	private ELayerScroll _body;

//	private UIcons _sys;

//	private const int _offset = 55;

//	private Padding _old;

//	private Rectangle _rx;

//	private Padding _cell;

//	private bool _fixed;

//	private string _rightText = "刷新";

//	public System.Drawing.Image RightIcon = Icos.Get(IcoEnums.fresh);

//	public System.Drawing.Image RightIconHover = Icos.Get(IcoEnums.fresh_hover);

//	private Size _maxold;

//	private Queue<EControl> _queue = new Queue<EControl>();

//	private readonly Dictionary<string, int> _dict = new Dictionary<string, int>();



//	public static ContextMenuStrip 停靠
//	{
//		get
//		{
//			if (_停靠 == null)
//			{
//				_停靠 = new ContextMenuStrip
//				{
//					Font = (ETheme.IsLarger ? ETheme.微软雅黑Tiny : ETheme.微软雅黑)
//				};
//				_停靠.Items.AddRange(new ToolStripItem[1]
//				{
//					new EToolStripMenuItem("停靠/窗口", Icos.Get(IcoEnums.copy), menu_Action)
//				});
//			}
//			return _停靠;
//		}
//	}

//	private int XWidth
//	{
//		get
//		{
//			if (!_fixed)
//			{
//				return 13;
//			}
//			return 0;
//		}
//	}

//	private bool IsFresh
//	{
//		get
//		{
//			return _isfresh;
//		}
//		set
//		{
//			if (_isfresh != value)
//			{
//				_isfresh = value;
//				if (value)
//				{
//					Cursor = Cursors.Hand;
//				}
//				Invalidate(FreshRect);
//			}
//		}
//	}

//	private Rectangle FreshRect
//	{
//		get
//		{
//			Point point = _sys.xLocation;
//			return new Rectangle(point.X - 55, point.Y + 2, 47, 18);
//		}
//	}

//	[Category("设计")]
//	[AmbientValue(typeof(Padding), "6, 4, 6, 4")]
//	public Padding CellPadding
//	{
//		get
//		{
//			return _cell;
//		}
//		set
//		{
//			_cell = value;
//		}
//	}

//	public bool IsForm => base.Parent is EFormOwner;

//	[Category("设计")]
//	[DefaultValue(false)]
//	public bool Fixed
//	{
//		get
//		{
//			return _fixed;
//		}
//		set
//		{
//			_fixed = value;
//		}
//	}

//	[Category("设计")]
//	[DefaultValue(true)]
//	public bool SysVisbile
//	{
//		get
//		{
//			return _sys.Visible;
//		}
//		set
//		{
//			if (_sys.Visible != value)
//			{
//				_sys.Visible = value;
//				Invalidate(FreshRect);
//			}
//		}
//	}

//	[Category("设计")]
//	[DefaultValue("刷新")]
//	public string RightText
//	{
//		get
//		{
//			return _rightText;
//		}
//		set
//		{
//			_rightText = value;
//			Invalidate(FreshRect);
//		}
//	}

//	protected override int HeadHeight => base.FontHeight + _cell.Vertical + TabPadding.Vertical + 2;

//	private int HeadRight => base.Width - TabPadding.Right - (_sys.Visible ? _sys.Width : 0);

//	private Rectangle HeadRect => new Rectangle(base.BorderLeft, 0, base.Width, HeadHeight);

//	protected Color HeadColor
//	{
//		get
//		{
//			if (!IsForm)
//			{
//				return ETheme.Current.ColorBackCtrl;
//			}
//			return ETheme.Current.ColorBackDark;
//		}
//	}

//	public EControl this[int idx]
//	{
//		get
//		{
//			if (idx < base.Pages.Count)
//			{
//				return base.Pages[idx];
//			}
//			return null;
//		}
//	}

//	public override EControl this[string key]
//	{
//		get
//		{
//			for (int i = 0; i < base.Pages.Count; i++)
//			{
//				EControl eControl = base.Pages[i];
//				if (eControl.Name == key)
//				{
//					return eControl;
//				}
//			}
//			return null;
//		}
//	}

//	[Browsable(false)]
//	[EditorBrowsable(EditorBrowsableState.Never)]
//	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
//	public override EControl Page
//	{
//		get
//		{
//			if (_body.Controls.Count != 0)
//			{
//				return _body.Controls[0] as EControl;
//			}
//			return null;
//		}
//		set
//		{
//			if (Page != value)
//			{
//				if (_body.Controls.Count > 0)
//				{
//					_dict[_body.Controls[0].Name] = _body.ScrollValue;
//				}
//				_body.Controls.Clear();
//				_body.ScrollTop();
//				if (value != null)
//				{
//					_body.ReHeight(value.Height);
//					_body.Controls.Add(value);
//					if (_dict.TryGetValue(value.Name, out var value2))
//					{
//						_body.ScrollValue = value2;
//					}
//				}
//				OnSelectedChanged();
//				Invalidate(HeadRect);
//			}
//			if (!Visible)
//			{
//				Visible = true;
//			}
//		}
//	}

//	public int SelectedIndex
//	{
//		get
//		{
//			EControl page = Page;
//			for (int i = 0; i < base.Pages.Count; i++)
//			{
//				if (base.Pages[i] == page)
//				{
//					return i;
//				}
//			}
//			return -1;
//		}
//		set
//		{
//			if (value >= 0 && value < base.Pages.Count)
//			{
//				Page = base.Pages[value];
//			}
//		}
//	}

//	[DefaultValue(true)]
//	public new bool Visible
//	{
//		get
//		{
//			if (!IsForm)
//			{
//				return base.Visible;
//			}
//			return base.Parent.Visible;
//		}
//		set
//		{
//			if (IsForm)
//			{
//				base.Parent.Visible = value;
//				if (value)
//				{
//					Form form = FindForm();
//					if (form.WindowState == FormWindowState.Minimized)
//					{
//						form.WindowState = FormWindowState.Normal;
//					}
//					form.Activate();
//				}
//			}
//			else
//			{
//				base.Visible = value;
//			}
//		}
//	}

//	public event EventHandler RightClick;

//	private static void menu_Action(object sender, EventArgs e)
//	{
//		if (_停靠.Tag is EPanelEx ePanelEx && sender is ToolStripMenuItem { Text: "停靠/窗口" })
//		{
//			if (ePanelEx.IsForm)
//			{
//				ePanelEx.DockControl();
//				return;
//			}
//			Point point = ePanelEx.PointToScreen(ePanelEx.Location);
//			ePanelEx.DockForm(new Point(point.X - 100, point.Y));
//		}
//	}

//	public EPanelEx()
//	{
//		_cell = new Padding(6, 4, 8, 4);
//		base.xPadding = new Padding(1, 0, 1, 1);
//		_sys = new UIcons(26, 24);
//		_sys.AddRange(new UIcon("min", IcoEnums.min, visible: false, Color.FromArgb(67, 139, 221), Color.FromArgb(47, 119, 201)), new UIcon("max", IcoEnums.max, visible: false, Color.FromArgb(67, 139, 221), Color.FromArgb(47, 119, 201)), new UIcon("close", IcoEnums.mclose, Color.FromArgb(180, 50, 50), Color.FromArgb(216, 70, 70)));
//	}

//	public override void ChangeTheme()
//	{
//		base.ChangeTheme();
//		BackColor = ((base.Parent is Form) ? ETheme.Current.ColorBackDark : ETheme.Current.ColorBackCtrl);
//		_body = new ELayerScroll
//		{
//			Dock = DockStyle.Fill
//		};
//		base.Controls.Add(_body);
//	}

//	private void DockForm(Point p)
//	{
//		_maxold = base.MaximumSize;
//		base.MaximumSize = Size.Empty;
//		_dockCtrl = base.Parent;
//		if (_dockForm == null)
//		{
//			_dockForm = new EFormOwner
//			{
//				StartPosition = FormStartPosition.Manual,
//				Location = p,
//				Text = Text
//			};
//		}
//		_sys.SetVisible(true, "min", "max");
//		Padding padding = (_old = TabPadding);
//		TabPadding = new Padding(padding.Left + 1, padding.Top + 1, padding.Right + 1, padding.Bottom + 1);
//		Size size = xSize;
//		Dock = DockStyle.Fill;
//		_dockForm.Controls.Add(this);
//		_dockForm.Show();
//		_dockForm.xSize = size;
//	}

//	private void DockControl()
//	{
//		EFormMask.Current.Hide();
//		_dockForm.Hide();
//		base.MaximumSize = _maxold;
//		BackColor = ETheme.Current.ColorBackCtrl;
//		Dock = DockStyle.Right;
//		_sys.SetVisible(false, "min", "max");
//		TabPadding = _old;
//		int num = ((base.Percent <= 0) ? base.xMinimumSize.Width : (_dockCtrl.Width * base.Percent / 100));
//		if (_dockCtrl.MaximumSize.Width > 0)
//		{
//			num = _dockCtrl.MaximumSize.Width;
//		}
//		if (num > _dockCtrl.Width - base.Distance)
//		{
//			num = _dockCtrl.Width - base.Distance;
//		}
//		base.Width = num;
//		_dockCtrl.Controls.Add(this);
//	}

//	private void ec_TextChanged(object sender, EventArgs e)
//	{
//		Invalidate();
//	}

//	public override EControl CreatePage(ISource src, string name)
//	{
//		EControl eControl = base[name];
//		if (eControl == null)
//		{
//			if (_queue.Count == 0)
//			{
//				eControl = base.OnCreate(src, name);
//			}
//			else
//			{
//				eControl = _queue.Dequeue();
//				eControl.ReplaceName(name);
//			}
//			Insert(eControl);
//		}
//		else if (eControl.Name != eControl.GetInnerName())
//		{
//			eControl.Load();
//		}
//		return eControl;
//	}

//	public EControl GetTopControl() { 
//		return Pages[0];
//	}


//	public void Insert(EControl ec)
//	{
//		if (!Visible)
//		{
//			if (base.Parent != null && base.Parent.Width > 0)
//			{
//				base.Width = base.Parent.Width * 40 / 100;
//				base.Percent = base.Width * 100 / base.Parent.Width;
//			}
//			Visible = true;
//		}
//		base.Pages.Insert(0, ec);
//		if (base.Pages.Count > 10)
//		{
//			int idx = base.Pages.Count - 1;
//			EControl eControl = this[idx];
//			if (eControl != null)
//			{
//				Remove(eControl);
//			}
//		}
//		if (ec != null)
//		{
//			ec.TextChanged += ec_TextChanged;
//		}
//		Page = ec;
//		ec.Load();
//		Invalidate();
//	}

//	public void Append(EControl ec)
//	{
//		if (!Visible)
//		{
//			base.Width = base.Parent.Width * 40 / 100;
//			base.Percent = base.Width * 100 / base.Parent.Width;
//			Visible = true;
//		}
//		base.Pages.Add(ec);
//		ec.TextChanged += ec_TextChanged;
//		Page = ec;
//		ec.Load();
//		Invalidate();
//	}

//	public void Remove(EControl ec)
//	{
//		base.Pages.Remove(ec);
//		_dict.Remove(ec.Name);
//		ec.TextChanged -= ec_TextChanged;
//		if (base.Pages.Count == 0)
//		{
//			Page = null;
//			if (_sys.Visible)
//			{
//				Visible = false;
//			}
//			base.OnRemove(ec);
//		}
//		else if (Page == ec)
//		{
//			OnChange(base.Pages[0]);
//		}
//		ec.Reset();
//		_queue.Enqueue(ec);
//	}

//	public override void Remove(string key)
//	{
//		EControl eControl = this[key];
//		if (eControl != null)
//		{
//			Remove(eControl);
//		}
//	}

//	public void ChangeAble(EControl ec) {
//		base.OnChange(ec);
//	}

//	public void Clear(bool isclose = true)
//	{
//		for (int i = 0; i < base.Pages.Count; i++)
//		{
//			EControl eControl = base.Pages[i];
//			base.OnRemove(eControl);
//			eControl.Reset();
//			_queue.Enqueue(eControl);
//		}
//		base.Pages.Clear();
//		_dict.Clear();
//		Page = null;
//		if (isclose)
//		{
//			Visible = false;
//		}
//	}

//    public void ClearPage(EControl con) {
//        //Pages.Remove(con);
//        base.OnRemove(con);
//        //con.Reset();
//        _queue.Enqueue(con);
//        //con.Reset();
//    }

//	public void Init()
//	{
//		base.Pages.Clear();
//		_dict.Clear();
//		_queue.Clear();
//	}

//	protected virtual void OnClose()
//	{
//		try
//		{
//			Clear();
//		}
//		catch
//		{
//		}
//	}

//	protected override void OnChange(EControl ec)
//	{
//		Page = ec;
//		base.OnChange(ec);
//	}

//	protected virtual void OnRightClick()
//	{
//		if (this.RightClick != null)
//		{
//			this.RightClick(this, EventArgs.Empty);
//		}
//		else if (RightText == "刷新")
//		{
//			Page?.Load();
//		}
//	}

//	protected override void OnResize(EventArgs e)
//	{
//		try
//		{
//			_sys.xLocation = new Point(HeadRight, (HeadHeight - _sys.Height) / 2);
//			PerformLayout();
//			Invalidate(HeadRect);
//		}
//		catch
//		{
//		}
//	}

//	protected override void OnMouseMove(MouseEventArgs e)
//	{
//		try
//		{
//			if (e.Button == MouseButtons.Left)
//			{
//				if (_rx.Contains(base.Mouse))
//				{
//					return;
//				}
//				if (base.Parent is Form form)
//				{
//					Point position = Cursor.Position;
//					form.Location = new Point(position.X - base.Mouse.X, position.Y - base.Mouse.Y);
//					if (_dockCtrl == null)
//					{
//						return;
//					}
//					Rectangle r = _dockCtrl.ClientRectangle;
//					if (r.Width > 30)
//					{
//						r = new Rectangle(r.Right - 30, r.Top, 30, r.Height);
//					}
//					r = _dockCtrl.RectangleToScreen(r);
//					EFormMask current = EFormMask.Current;
//					if (r.Contains(position))
//					{
//						if (!current.Visible)
//						{
//							current.Size = xSize;
//							current.Location = new Point(r.Right - base.Width, r.Top);
//							current.Show();
//						}
//					}
//					else if (current.Visible)
//					{
//						current.Hide();
//					}
//				}
//				else
//				{
//					Point location = e.Location;
//					if (base.LastSplit != Rectangle.Empty)
//					{
//						DrawSplitHelper(location);
//					}
//				}
//				return;
//			}
//			Point location2 = e.Location;
//			if (_rx.Contains(location2))
//			{
//				return;
//			}
//			if (Dock == DockStyle.Right && base.BorderRect.Contains(e.Location))
//			{
//				Cursor = Cursors.SizeWE;
//				return;
//			}
//			if (FreshRect.Contains(location2))
//			{
//				IsFresh = true;
//				return;
//			}
//			IsFresh = false;
//			if (_sys.MouseMove(e))
//			{
//				Cursor = Cursors.Hand;
//				Invalidate(_sys.DisplayRectangle);
//				return;
//			}
//			_ = ETheme.Current;
//			_ = Page;
//			int num = base.BorderLeft + TabPadding.Left;
//			int num2 = HeadRight - 55;
//			int num3 = num;
//			int y = (int)Math.Ceiling((double)TabPadding.Top + (double)((base.FontHeight + _cell.Vertical - 11) / 2));
//			_hover = null;
//			if (e.Y > HeadHeight)
//			{
//				Cursor = Cursors.Default;
//				return;
//			}
//			foreach (EControl page in base.Pages)
//			{
//				int num4 = page.TextWidth + _cell.Horizontal + XWidth;
//				if (num3 + num4 >= num2)
//				{
//					break;
//				}
//				if (e.X >= num3 && e.X < num3 + num4)
//				{
//					_hover = page;
//					Cursor = Cursors.Hand;
//					Point location3 = new Point(num3 + num4 - _cell.Right / 2 - XWidth, y);
//					Rectangle rx = new Rectangle(location3, new Size(XWidth, XWidth));
//					if (rx.Contains(e.Location))
//					{
//						_rx = rx;
//						if (_xc != page)
//						{
//							_xc = page;
//							Invalidate(HeadRect);
//							return;
//						}
//						break;
//					}
//					break;
//				}
//				num3 += num4;
//			}
//			if (_xc != null)
//			{
//				_rx = default(Rectangle);
//				_xc = null;
//				Invalidate(HeadRect);
//			}
//			if (_hover == null)
//			{
//				Cursor = Cursors.Default;
//			}
//		}
//		catch
//		{
//		}
//	}

//	protected override void OnMouseDown(MouseEventArgs e)
//	{
//		try
//		{
//			base.OnMouseDown(e);
//			if (e.Button == MouseButtons.Left)
//			{
//				停靠.Hide();
//				_isdown = true;
//				if (_xc != null)
//				{
//					Invalidate(HeadRect);
//				}
//				else if (_sys.MouseDown())
//				{
//					Invalidate(_sys.DisplayRectangle);
//				}
//				else if (_hover != null && _hover != Page)
//				{
//					OnChange(_hover);
//				}
//			}
//			else if (e.Button == MouseButtons.Right && HeadRect.Contains(e.Location))
//			{
//				停靠.Tag = this;
//				停靠.Show(PointToScreen(e.Location));
//			}
//		}
//		catch
//		{
//		}
//	}

//	protected override void OnMouseUp(MouseEventArgs e)
//	{
//		try
//		{
//			_isdown = false;
//			base.OnMouseUp(e);
//			if (e.Button != MouseButtons.Left)
//			{
//				return;
//			}
//			if (_xc != null)
//			{
//				if (!_rx.Contains(e.Location))
//				{
//					_xc = null;
//				}
//				Invalidate(HeadRect);
//			}
//			else if (_sys.MouseUp())
//			{
//				Invalidate(_sys.DisplayRectangle);
//			}
//			if (EFormMask.Current.Visible)
//			{
//				DockControl();
//			}
//		}
//		catch
//		{
//		}
//	}

//	protected override void OnMouseLeave(EventArgs e)
//	{
//		if (_rx.Left > 0)
//		{
//			ETheme current = ETheme.Current;
//			EControl page = Page;
//			if (!_fixed)
//			{
//				using Graphics g = CreateGraphics();
//				Color color = ((page == _hover) ? current.ColorBorderFocus : current.ColorBorder);
//				Color borderColor = ((page == _hover) ? current.ColorTextBack : HeadColor);
//				EPaint.DrawClose(g, _rx.Location, color, borderColor);
//			}
//		}
//		else if (_sys.Leave())
//		{
//			Invalidate(_sys.DisplayRectangle);
//		}
//		_hover = null;
//		_rx = Rectangle.Empty;
//		Cursor = Cursors.Default;
//	}

//    private int coursl = 0;


//    protected override void OnMouseClick(MouseEventArgs e)
//	{
//		try
//		{
//			Point location = e.Location;
//			if (_rx.Contains(location))
//			{
//				Remove(_hover);
//				_rx = Rectangle.Empty;
//				_hover = null;
//				Invalidate(HeadRect);
//				return;
//			}
//			if (FreshRect.Contains(location))
//			{
//				OnRightClick();
//				return;
//			}

//			if ((base.Parent.Name == "采集列表" || base.Parent.Name == "产品列表")
//            && e.Button == MouseButtons.Left) {
//				Point point = _sys.Location;
//				Rectangle c =  new Rectangle(point.X - 150, point.Y + 4, 30, 30);
				
//                ETurn eTurn = null;
//                EGallery eGallery = null;
//                foreach (EControl con in base.Parent.Controls[0].Controls)
//                {
//                    if (con is EGallery eg && eg.Name.Equals("grid"))
//                    {
//                        eGallery = eg;
//                    }
//                    if (con is ETurn a && a.Name.Equals("PTurn"))
//                    {
//                        eTurn = a;
//                    }
//                }

//                if (c.Contains(location) && false) {

//					if (eTurn != null) {
//                        int num = (eGallery.MaxCount - 1) / eTurn.PageSize + 1;
//                        if (eTurn.Page <= num)
//                        {
//                            eTurn.Page = num;
//                            eTurn.MyChange();
//                        }
//                    }
                    

//                    if (eGallery != null) {
//                        for (int i = 0; i < 10; i++)
//                        {
//                            int currentNum = processCourse(eGallery, eTurn);
//                            this.CreatePage(eGallery, eGallery.Cells[currentNum].Name);
//                        }
//                        //MouseEventArgs m = new MouseEventArgs(MouseButtons.Left, 1, _rx.X, _rx.Y, 0);
//                        //foreach (ICell cell in eGallery.Cells)
//                        //{
//                        //Point p = new Point(cell.ClientRectangle.X, cell.ClientRectangle.Y);
//                        //eGallery.MouseClickLeft(p);
//                        //Thread.Sleep(1000);
//                        //this.CreatePage(eGallery, cell.Name);

//                        //                     Thread.Sleep(100);
//                        //this.Refresh();
//                        //                     Thread.Sleep(100);
//                        //                     this.OnMouseClick(m);
//                        //                     Thread.Sleep(1000);

//                        //                     this.OnMouseUp(m);
//                        //                     Thread.Sleep(1000);

//                        //foreach(EControl page in Pages)
//                        //{
//                        //	page.Load();
//                        //}


//                        //}

//                        //                 for (int i = 0; i < eGallery.Cells.Count; i++) {
//                        //MouseEventArgs m = new MouseEventArgs(MouseButtons.Left, 1, _rx.X, _rx.Y, 0);
//                        //                     this.OnMouseClick(m);
//                        //                     this.OnMouseUp(m);
//                        //Thread.Sleep(5000);
//                        //                     Page?.Load();
//                        //                 }
//                    }

//					//EButton submit = null;
//					//foreach (EControl con in Controls[0].Controls[0].Controls) {
//					//	if (con.Name.Equals("btnSubmit1") && con is EButton btn ) {
//					//		submit = btn;
//     //                   }
//					//}
//                }



//                //库存
//                Rectangle d = new Rectangle(point.X - 100, point.Y + 4, 30, 30);
//				if (d.Contains(location)) {

//					EStockEdit table = null;
//					EButton button = null;
//					EText all = null;

//					foreach (EControl con in this.Controls[0].Controls[0].Controls)
//					{
//						if (con is EStockEdit eg && eg.Name.Equals("vtbl"))
//						{
//							table = eg;
//						}
//						if (con is EButton btn1 && btn1.Name.Equals("btnSubmit1"))
//						{
//							button = btn1;
//						}
//						if (con is ZYing.Detail.SaleLayer sl && sl.Name.Equals("inner"))
//						{

//							foreach (EControl con1 in sl.Controls)
//							{

//								if (con1 is ETextUnit tu && tu.Name.Equals("txtNum"))
//								{

//									foreach (EControl con2 in con1.Controls)
//									{

//										if (con2 is EText tu2)
//										{
//											all = tu2;
//											break;
//										}
//									}
//									break;
//								}

//							}
//						}
//					}


//					if (table != null) {

//						JsonArray config = JsonArray.Create(File.ReadAllText("stock.json"));
//						bool modify = true;
//						foreach (ETRow row in table.Rows) {
//							foreach (ETCell cell in row.Cells) {
//								foreach (JsonElement item in config)
//								{
//									if (cell.Name == item.GetString("key")) {
//										string numV = item.GetString("value");
//										if (numV.Contains("-"))
//										{
//											string[] split = numV.Split("-");
//											int min = int.Parse(split[0]);
//											int max = int.Parse(split[1]);
//											if (min > max)
//											{
//												int temp = min;
//												min = max;
//												max = temp;
//											}
//											int randomNumber = new Random().Next(min, max);
//											numV = randomNumber.ToString();
//										}

//										cell.Text = numV;
//										modify = false;
//									}
//								}


//							}



//						}

//						if (modify && all != null) {

//							foreach (JsonElement item in config)
//							{
//								if (!item.GetString("key").Equals("num")) {
//									continue;
//								}
//								string numV = item.GetString("value");
//								if (numV.Contains("-"))
//								{
//									string[] split = numV.Split("-");
//									int min = int.Parse(split[0]);
//									int max = int.Parse(split[1]);
//									if (min > max)
//									{
//										int temp = min;
//										min = max;
//										max = temp;
//									}
//									int randomNumber = new Random().Next(min, max);
//									numV = randomNumber.ToString();
//								}
//								all.Text = numV;
//							}
//						}
//					}

				
//                    int currentNum = processCourse(eGallery, eTurn);
//                    this.CreatePage(eGallery, eGallery.Cells[currentNum].Name);
//                    button.MyClick();
//                }



//            }

//            switch (_sys.GetCommand())
//			{
//			case "min":
//				if (base.Parent is EFormOwner)
//				{
//					((EFormOwner)base.Parent).WindowState = FormWindowState.Minimized;
//				}
//				break;
//			case "max":
//				if (base.Parent is EFormOwner)
//				{
//					EFormOwner eFormOwner = base.Parent as EFormOwner;
//					if (eFormOwner.WindowState != FormWindowState.Maximized)
//					{
//						eFormOwner.WindowState = FormWindowState.Maximized;
//						_sys["max"].DefaultImage = Icos.Get(IcoEnums.maxx);
//					}
//					else
//					{
//						eFormOwner.WindowState = FormWindowState.Normal;
//						_sys["max"].DefaultImage = Icos.Get(IcoEnums.max);
//					}
//				}
//				break;
//			case "close":
//				OnClose();
//				break;
//			}
//		}
//		catch
//		{
//		}
//	}

//	private int processCourse(EGallery eGallery, ETurn eTurn) {
//		//处理转场
//		if (eTurn != null)
//		{
//			int page = coursl / eTurn.PageSize + 1;
//			if (page != eTurn.Page) {
//                eTurn.Page = page;
//                eTurn.MyChange();
//                Thread.Sleep(500);
//            }
//        }
//		int currentNum = coursl % eTurn.PageSize;
//        if (coursl < eGallery.MaxCount)
//        {	
//            coursl++;
//        }
//        else
//        {
//            coursl = 0;
//        }
//		return currentNum;
//    }

//	protected override void OnMouseDoubleClick(MouseEventArgs e)
//	{
//		if (IsForm && e.Location.Y < HeadHeight)
//		{
//			Form form = FindForm();
//			form.WindowState = ((form.WindowState == FormWindowState.Normal) ? FormWindowState.Maximized : FormWindowState.Normal);
//		}
//	}

//	protected override void OnPaintBackground(PaintEventArgs e)
//	{
//		if (!IsForm)
//		{
//			base.OnPaintBackground(e);
//			return;
//		}
//		Graphics graphics = e.Graphics;
//		graphics.Clear(BackColor);
//		using Brush brush = new SolidBrush(ETheme.Current.ColorBackCtrl);
//		graphics.FillRectangle(brush, new Rectangle(base.BorderLeft, 0, base.Width - base.BorderLeft, HeadHeight - 2));
//	}

//	protected override void OnPaint(PaintEventArgs e)
//	{
//		Graphics graphics = e.Graphics;
//		ETheme current = ETheme.Current;
//		_sys.Draw(graphics);
//		Point point = _sys.xLocation;
//        System.Drawing.Image image = (_isfresh ? RightIconHover : RightIcon);
//		EPaint.Draw(graphics, image, new Rectangle(point.X - 55, point.Y + 4, image.Width, image.Height));
//		EPaint.DrawText(graphics, _rightText, current.ColorText, new Point(point.X - 55 + 20, point.Y + 2));

//		if (base.Parent.Name == "采集列表" || base.Parent.Name == "产品列表") { 
//			//EPaint.DrawText(graphics, "一键", current.ColorText, new Point(point.X -150, point.Y + 2));
//            EPaint.DrawText(graphics, "库存", current.ColorText, new Point(point.X - 100, point.Y + 2));
//        }
//		int headHeight = HeadHeight;
//		int borderLeft = base.BorderLeft;
//		EControl page = Page;
//		using Pen pen = new Pen(current.ColorGrid);
//		using Pen pen2 = new Pen(current.ColorTextBack);
//		int x = base.Width - borderLeft + 1;
//		graphics.DrawLine(pen, borderLeft, headHeight - 2, x, headHeight - 2);
//		graphics.DrawLine(pen2, borderLeft, headHeight - 1, x, headHeight - 1);
//		borderLeft += TabPadding.Left;
//		using (new SolidBrush(ForeColor))
//		{
//			int num = base.FontHeight + _cell.Vertical;
//			int y = (int)Math.Ceiling((double)TabPadding.Top + (double)((num - 11) / 2));
//			int num2 = HeadRight - 55;
//			for (int i = 0; i < base.Pages.Count; i++)
//			{
//				EControl eControl = base.Pages[i];
//				x = eControl.TextWidth + _cell.Horizontal + XWidth;
//				if (borderLeft + x >= num2)
//				{
//					if (eControl == page)
//					{
//						base.Pages.Remove(eControl);
//						base.Pages.Insert(0, eControl);
//						Invalidate(HeadRect);
//						break;
//					}
//				}
//				else
//				{
//					Rectangle r = new Rectangle(borderLeft, TabPadding.Top, x, num - 1);
//					if (eControl == page)
//					{
//						EPaint.DrawRect(graphics, r);
//					}
//					else if (i > 0 && page != base.Pages[i - 1])
//					{
//						graphics.DrawLine(pen, borderLeft, TabPadding.Top + _cell.Top, borderLeft, TabPadding.Top + _cell.Top + base.FontHeight);
//						graphics.DrawLine(pen2, borderLeft + 1, TabPadding.Top + _cell.Top, borderLeft + 1, TabPadding.Top + _cell.Top + base.FontHeight);
//					}
//					EPaint.DrawText(graphics, eControl.Text, current.ColorText, new Rectangle(r.Left + _cell.Left, r.Top + _cell.Top, r.Width - _cell.Horizontal, r.Height - _cell.Vertical));
//					Color color;
//					Color borderColor;
//					if (eControl == _xc)
//					{
//						color = (_isdown ? Color.Red : current.ColorBorderFocus);
//						borderColor = (_isdown ? Color.Red : current.ColorBorderFocus);
//					}
//					else
//					{
//						color = ((eControl == page) ? current.ColorBorderFocus : current.ColorBorder);
//						borderColor = ((eControl == page) ? current.ColorTextBack : HeadColor);
//					}
//					if (!_fixed)
//					{
//						EPaint.DrawClose(graphics, new Point(r.Right - _cell.Right / 2 - XWidth, y), color, borderColor);
//					}
//				}
//				borderLeft += x;
//			}
//		}
//	}

//    // 导入 user32.dll 中的 keybd_event 函数
//    [DllImport("user32.dll", SetLastError = true)]
//    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

//    // 定义键盘事件标志
//    private const uint KEYEVENTF_KEYDOWN = 0x0000;
//    private const uint KEYEVENTF_KEYUP = 0x0002;

//    // 定义 'Y' 键的虚拟键码
//    private const byte VK_Y = 0x59;

//    public static void KeyDown()
//    {
//        // 等待 2 秒，给用户切换到目标输入框的时间
//        Thread.Sleep(2000);

//        // 模拟按下 'Y' 键
//        keybd_event(VK_Y, 0, KEYEVENTF_KEYDOWN, 0);

//        // 短暂延迟，模拟按键按下的时长
//        Thread.Sleep(100);

//        // 模拟释放 'Y' 键
//        keybd_event(VK_Y, 0, KEYEVENTF_KEYUP, 0);
//    }


//    public static void MouseLeft(Point point)
//    {
//        // 模拟鼠标移动到指定位置，这里以坐标 (500, 500) 为例
//        Cursor.Position = point;

//        // 短暂延迟，确保鼠标移动到位
//        Thread.Sleep(200);

//        // 模拟鼠标左键按下
//        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);

//        // 短暂延迟，模拟按下的时长
//        Thread.Sleep(100);

//        // 模拟鼠标左键释放
//        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
//    }

//    // 导入 user32.dll 中的 mouse_event 函数
//    [System.Runtime.InteropServices.DllImport("user32.dll")]
//    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

//    // 定义鼠标事件标志
//    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
//    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
//}
