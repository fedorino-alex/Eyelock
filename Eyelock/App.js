if (!window.Eyelock)
	window.Eyelock = {};

Eyelock.App = function (settings)
{
	this._settings =
    {
    	AutoUpdate: false,
    	AutoUpdateInterval: 5000, //ms
    	CurrentItem: null,
    	UpdateCallback: webix.bind(this._onUpdateCallback, this)
    };

	this._updater = new Eyelock.AutoUpdater(this._settings);
	if (this._settings.AutoUpdate)
		webix.assert(this._updater.Start(), 'Autoupdater not started...');
};

var appP = Eyelock.App.prototype;

appP.init = function ()
{
	this._initPopupSettings();
	this._initLayout();
	this._bind();

	this.goToNextListItem();
};

appP._initPopupSettings = function ()
{
	webix.ui(
	   {
	   	id: 'settings_popup',
	   	view: 'popup',
	   	head: false,
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

appP._initLayout = function ()
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
						template: function (obj, common)
						{
							obj.cheight = obj.dheight = 48;
							return "<div class='webix_el_box' style='width:52px; height:54px; margin: 13px 14px;'>" + common._inputTemplate(obj, common) + "</div>";
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
					{ view: "resizer" },
					{
						rows:
						[{
							id: "tab_view",
							view: "multiview",
							animate: false,
							cells:
							[
								{ id: 'null' },
								this._getAddForm(),
								this._getPreviewForm()
							]
						},
						{
							view: 'button',
							id: 'processButton',
							value: 'Обработать',
							inputWidth: 150,
							enabled: false,
							align: 'right'
						}]
					}
				]
			}
		]
	});
};

appP._getAddForm = function ()
{
	var form =
    {
    	id: 'add',
    	view: 'layoutform',
		type: 'line',
    	rows:
		[
			{ view: 'label', type: 'header', label: '<span class="FormTitle">Add user profile</span>' },
    		{
    			autoheight: true,
    			cols:
				[
					{
						view: 'form',
						elements:
						[
							{
								view: 'image',
								name: 'rightIris',
								borderless: true,
								height: 144,
								align: 'center'
							}
						]
					},
					{
						view: 'form',
						elements:
						[
							{
								view: 'image',
								name: 'leftIris',
								borderless: true,
								height: 144,
								align: 'center'
							}
						]
					}
				]
    		},
			{
				view: 'form',
				elements:
				[
					{ view: 'text', label: 'First name:', name: 'firstName', labelWidth: 180 },
					{ view: 'text', label: 'Last name:', name: 'lastName', labelWidth: 180 },
					{ view: 'datepicker', label: 'Date of birth:', name: 'dob', labelWidth: 180 },
				],
				rules:
				{
					firstName: webix.rules.isNotEmpty,
					lastName: webix.rules.isNotEmpty,
					dob: webix.rules.isNotEmpty
				},
				on:
				{
					onValidationError: function (id, value)
					{
						var text;

						if (id == "lastName")
							text = "Last name can't be empty";
						if (id == "firstName")
							text = "First name can't be empty";
						if (id == "dob")
							text = "Date of birth can't be empty";

						webix.message({ type: "error", text: text });
					}
				}
			},
			{}
    	]
    };
	return form;
};

appP._getPreviewForm = function ()
{
	var form =
    {
    	id: 'view',
    	view: 'layoutform',
    	rows:
		[
			{ view: 'label', type: 'header', label: '<span class="FormTitle">Preview user profile</span>' },
    		{
    			autoheight: true,
    			cols:
				[
					{
						view: 'form',
						elements:
						[
							{
								view: 'image',
								name: 'rightIris',
								borderless: true,
								height: 144,
								align: 'center'
						}
						]
					},
					{
						view: 'form',
						elements:
						[
							{
								view: 'image',
								name: 'leftIris',
								borderless: true,
								height: 144,
								align: 'center'
							}
						]
					}
				]
    		},
			{
				view: 'form',
				elements:
				[
					{ view: 'text', label: 'First name:', name: 'firstName', labelWidth: 180 },
					{ view: 'text', label: 'Last name:', name: 'lastName', labelWidth: 180 },
					{ view: 'datepicker', label: 'Date of birth:', name: 'dob', labelWidth: 180 },
				],
				rules:
				{
					firstName: webix.rules.isNotEmpty,
					lastName: webix.rules.isNotEmpty,
					dob: webix.rules.isNotEmpty
				},
				on:
				{
					onValidationError: function (id, value)
					{
						var text;

						if (id == "lastName")
							text = "Last name can't be empty";
						if (id == "firstName")
							text = "First name can't be empty";
						if (id == "dob")
							text = "Date of birth can't be empty";

						webix.message({ type: "error", text: text });
					}
				}

			},
			{}
		]
    };
	return form;
};

appP._bind = function ()
{
	// набор связываний и обработчиков событий
	var list = $$("queue");
	$$("add").bind(list, null, function (data) { return data ? data.data : null; });
	$$("view").bind(list, null, function (data) { return data ? data.data : null; });
	$$("tab_view").bind(list, "event", function (data) { return data || { event: 'empty' }; });

	list.attachEvent('onSelectChange', webix.bind(this._onQueueSelectionChanged, this));
	$$('autoUpdateCheckbox').attachEvent('onChange', webix.bind(this._onAutoUpdateChanged, this));
	$$('processButton').attachEvent('onItemClick', webix.bind(this._onProcessClick, this));
};

appP.goToNextListItem = function ()
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

appP._doProcess = function ()
{
	var list = $$("queue"),
        current = list.getCursor(),
        currentItem = $$('queue').getSelectedItem(),
        form;

	if (currentItem && currentItem.event)
		form = $$(currentItem.event);

	if (form.validate())
	{
		if (current)
			list.data.getItem(current).processed = true;

		// слать запрос.

		list.refresh();
		return true;
	}

	return false;
};

appP._appendNewEvents = function (events)
{
	var list = $$('queue');


};

// ------------------- Event handlers -------------------------

appP._onAutoUpdateChanged = function (value)
{
	if (this._settings.AutoUpdate = !!value)
		this._updater.Start();
	else
		this._updater.Stop();
};

appP._onProcessClick = function ()
{
	if (this._doProcess())
		this.goToNextListItem();
};

appP._onQueueSelectionChanged = function (sender, args)
{
	var list = $$("queue");
	this._settings.CurrentItem = list.getSelectedItem();
	list.getSelectedId() ? $$('processButton').enable() : $$('processButton').disable();
};

appP._onUpdateCallback = function (sender, args)
{
	// отправить запрос на получение новых элементов.
	console.log('Send update request: timestamp = ' + args.timestamp);
};

appP._onUpdateRequestSuccess = function (sender, args)
{
};

appP = null;