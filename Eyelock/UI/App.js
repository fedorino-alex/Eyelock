﻿if (!window.Eyelock)
	window.Eyelock = {};

Eyelock.App = function (settings)
{
	this._settings =
    {
    	AutoUpdate: false,
    	AutoUpdateInterval: 5000, //ms
    	CurrentItem: null,
    	UpdateCallback: webix.bind(this._onUpdateCallback, this),
    	GetNewEvents: "Service/QueueService.asmx/GetNewEvents",
    	GetAllEvents: "Service/QueueService.asmx/GetAllEvents",
    	ProcessEvent: "Service/QueueService.asmx/ProcessEvent"
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
	   	width: 240,
	   	body:
		{
			rows:
			[
				{
					view: 'checkbox',
					id: 'autoUpdateCheckbox',
					label: 'Включить автообновление',
					value: this._settings.AutoUpdate,
					labelWidth: 200
				}
			]
		}
	   }).hide();
};

appP._initLayout = function ()
{
	webix.ui({
		rows:
        [
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
						data: [],
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
								{ id: 'null', borderless: false },
								this._getAddForm(),
								this._getPreviewForm()
							]
						},
						{
							borderless: false,
							view: 'button',
							id: 'processButton',
							value: 'Обработать',
							inputWidth: 150,
							enabled: false,
							align: 'right',
							hotkey: 'enter'
						}]
					}
				]
			}
        ]
	});
};

appP._getForm = function (title, id)
{
	var form =
    {
    	id: id,
    	view: 'layoutform',
    	type: 'line',
    	rows:
		[
			{ view: 'label', type: 'header', label: '<span class="FormTitle">' + title + '</span>', borderless: false },
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
					{ view: 'text', label: 'Имя:', name: 'firstName', labelWidth: 180 },
					{ view: 'text', label: 'Фамилия:', name: 'lastName', labelWidth: 180 },
					{ view: 'datepicker', label: 'Дата рождения:', name: 'dob', labelWidth: 180 },
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
							text = "Фамилия не указана";
						if (id == "firstName")
							text = "Имя не указано";
						if (id == "dob")
							text = "Дата рождения не указана";

						webix.message({ type: "error", text: text });
					}
				}
			},
			{}
		]
    };

	return form;
};

appP._getAddForm = function ()
{
	return this._getForm('Добавление профиля', 'add');
};

appP._getPreviewForm = function ()
{
	return this._getForm('Просмотр профиля', 'view');
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

	this._onUpdateCallback(this, { url: this._settings.GetAllEvents });
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

		appP._sendProcessRequest(form.getValues());
		return true;
	}

	return false;
};

appP._appendNewEvents = function (events)
{
	var list = $$('queue');
	if (events)
		for (var i = 0; i < events.length; i++)
			list.data.add(events[i]);

	list.refresh();
};

appP._parseEvents = function (events)
{
	var event = null, result = [];
	events = events.d || events;
	if (events.IsSuccess)
{
		events = events.Result;
		for (var i = 0; i < events.length; i++)
{
			event = events[i];

			delete event["__type"];
			delete event["ExtensionData"];
			delete event.data["__type"];
			delete event.data["ExtensionData"];

			result.push(event);
		}
	}

	return result;
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
	var current = this._settings.CurrentItem = list.getSelectedItem();
	(list.getSelectedId() && current && !current.processed) ?
        $$('processButton').enable() :
        $$('processButton').disable();
};

appP._onUpdateCallback = function (sender, args)
{
	// отправить запрос на получение новых элементов.
	webix
        .ajax()
        .header({ "Content-Type": "application/json" })
        .post(args.url, null,
        {
        	success: webix.bind(this._onUpdateRequestSuccess, this),
        	error: webix.assert_error
        });
};

appP._onUpdateRequestSuccess = function (text, data)
{
	var events = this._parseEvents(data.json());

	if (events)
		this._appendNewEvents(events);
};

appP._sendProcessRequest = function (event)
{
	webix
        .ajax()
        .header({ "Content-Type": "application/json" })
        .post(this._settings.ProcessEvent, event,
        {
        	success: webix.bind(this._onProcessed, this),
        	error: webix.assert_error
        });
};

appP._onProcessed = function (result, args)
{
	// пометить как успешно обработанное ???
};

appP = null;