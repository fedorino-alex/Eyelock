var App = function (settings) 
{
    this._settings = 
    {
        AutoUpdate: true,
        AutoUpdateInterval: 5000, //ms
        CurrentItem: null
    };
};

var appP = App.prototype;

appP.init = function()
{
    this._initPopupSettings();
    this._initLayout();
    this._bind();

    this.goToNextListItem();
};

appP._initPopupSettings = function()
{
     webix.ui(
        {
            id: 'settings_popup',
            view: 'popup',
            head:false, 
            width: 200,
            body:
            {
                rows: 
                [
                    {
                        view: 'checkbox', 
                        id: 'autoUpdateCheckbox',
                        label: 'Enable auto update',
                        value: true,
                        labelWidth: 160
                    }
                ]
			}
        }).hide();
};

appP._initLayout = function()
{
	webix.ui({
		rows: [
			{ 
				cols: 
				[
					{
						height: 80, 
						view: "label",  
						label: "<span class='TitleLabel'>Eyelock query management application</span>",
						type: 'header'
					},
					{
						view: 'button',
                        width: 80,
                        type: 'image',
                        template:function(obj, common)
                        {
                            obj.cheight = obj.dheight = 48;
			                return "<div class='webix_el_box' style='width:52px; height:54px; margin: 13px 14px;'>"+common._inputTemplate(obj, common)+"</div>";
		                },
                        image: 'img/settings.png',
                        popup: 'settings_popup'
					}
				]
			},
			{ 
				cols: [
					{ 
						view: "list",
						id: "queue",
						minWidth: 320,
						width: 320,
						data: example_data,
						template: "<div><b>#title#</b><div>#data.firstName# #data.lastName# #data.dob#</div></div>",
						select: true,
						type: 
						{
							height: 'auto',
							classname: function (obj)
							{
								var css = webix.ui.list.prototype.type.classname.apply(this, arguments);
								if (obj && obj.processed)
									css = css + ' processed';
								return css;
							}
						},
					},
					{ view : "resizer" },
					{
						rows: 
						[{
							id: "tab_view",
							view: "multiview",
							cells: 
							[
								{ id: 'null', template: 'EMPTY' },
								this._getAddForm(),
								this._getPreviewForm()
							]
						},
						{
							cols: 
							[
								{
									view: 'button',
                                    id: 'processButton',
									value: 'Обработать',
									inputWidth: 150,
									enabled: false,
                                    align: 'right'
								}
							]
					}]
					}
				]
			}
		]
    });
};


appP._getAddForm = function()
{
    var form = { id:'view', view: 'form', elements: [{ view: 'checkbox', label: 'Checkbox' }] };
    return form;
};

appP._getPreviewForm = function()
{
    var form = { id:'add', view: 'form', elements: [{}] };
    return form;
};

appP._bind = function()
{
    // набор связываний и обработчиков событий

    $$("tab_view").bind($$("queue"), "event", function(data) { return data || { event: 'empty'}; });
    $$('autoUpdateCheckbox').attachEvent('onChange', this._onAutoUpdateChanged.bind(this));
    $$('processButton').attachEvent('onItemClick', this._onProcessClick.bind(this));

};

appP.goToNextListItem = function()
{
    var list = $$("queue"),
        current = list.getCursor() || list.data.getFirstId();

    while (current && list.data.getItem(current).processed)
        current = list.data.getNextId(current);

    if (current)
        list.select(current);
    else 
        list.unselectAll();
};

appP._doProcess = function()
{
    var list = $$("queue"),
        current = list.getCursor();

    if (current)
        list.data.getItem(current).processed = true;

    // слать запрос.

    list.refresh();
};

// ------------------- Event handlers -------------------------

appP._onAutoUpdateChanged = function(value)
{
    this._settings.AutoUpdate = !!value;
};

appP._onProcessClick = function()
{
    this._doProcess();
    this.goToNextListItem();
};

appP = null;