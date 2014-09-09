if (!window.Eyelock)
	window.Eyelock = {};

Eyelock.Enroll = function (settings)
{
	webix.require("../ServiceSettings.js");

	this._settings =
    {
    	AutoUpdate: false,
    	AutoUpdateInterval: 1000, //ms
    	CurrentItem: null,
    	UpdateCallback: webix.bind(this._onUpdateCallback, this),
    	GetNewEvents: Eyelock.Service.GetNewEvents
    };

	this._updater = new Eyelock.AutoUpdater(this._settings);
	if (this._settings.AutoUpdate)
		webix.assert(this._updater.Start(), 'Autoupdater not started...');
};

var appP = Eyelock.Enroll.prototype;

appP.init = function ()
{
	this._initPopupSettings();
	this._initLayout();
	this._bind();
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
					    borderless: false,
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
                            cols: [
                            {
                            },
                            {
                                view: 'button',
                                id: 'removeButton',
                                value: 'Удалить',
                                inputWidth: 150,
                                enabled: false,
                                align: 'right',
                                hotkey: 'del',
                                width: 150
                            },{
                                view: 'button',
                                id: 'processButton',
                                value: 'Обработать',
                                inputWidth: 150,
                                enabled: false,
                                align: 'right',
                                hotkey: 'enter',
                                width: 200
                            }]
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
								name: 'leftIris',
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
                            	name: 'rightIris',
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
					{ view: 'datepicker', label: 'Дата рождения:', name: 'dob', labelWidth: 180, format: "%d-%m-%Y" },
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
	$$('removeButton').attachEvent('onItemClick', webix.bind(this._onRemoveClick, this));

	this._onUpdateCallback(this, Eyelock.Service.GetAllEvents);
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
        currentItem = list.getSelectedItem(),
        form;

	if (currentItem && currentItem.event)
		form = $$(currentItem.event);

	if (form.validate())
	{
		var values = webix.copy(this._getValues(form, list));
		this._sendProcessRequest(this._prepareValues(values));
		webix.extend(currentItem.data, values.data, true);
	}
};

appP._doRemove = function ()
{
	var list = $$("queue"),
        current = list.getCursor(),
		currentItem = list.data.getItem(current);

    if (currentItem && currentItem.event.processed)
        return;

    this._sendRemoveRequest(this._prepareValues(currentItem));
};

appP._prepareValues = function (values)
{
	values.data =
        {
        	dob: webix.Date.dateToStr("%d-%m-%Y", false)(values.dob),
        	firstName: values.firstName,
        	lastName: values.lastName
        };

	delete values.dob;
	delete values.firstName;
	delete values.lastName;
	delete values.leftIris;
	delete values.rightIris;

	return values;
};

appP._getValues = function (form, list)
{
	var values = form.getValues();
	var listObject = list.getSelectedItem();
	return webix.extend(values, { uid: listObject.uid, event: listObject.event }, true);
};

appP._appendNewEvents = function (events)
{
	var list = $$('queue');
	if (events)
		for (var i = 0; i < events.length; i++)
			list.data.add(events[i]);

	if (!list.getSelectedItem())
		this.goToNextListItem();

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

			event.data.firstName = event.data.firstName || "";
			event.data.lastName = event.data.lastName || "";
			event.data.dob = event.data.dob || "";

			result.push(event);
		}
	}
	else
	{
		result = events.ErrorMessage;
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
    this._doProcess();
};

appP._onRemoveClick = function ()
{
    webix.confirm({
        text: 'Действительно хотите удалить из очереди необработанное событие?', ok: 'Да', cancel: 'Нет', callback: webix.bind(
        function ()
        {
            var list = $$("queue");
            this._doRemove();
        }, this)
    });
};

appP._onQueueSelectionChanged = function (sender, args)
{
    var list = $$("queue");
    var current = this._settings.CurrentItem = list.getSelectedItem();
    (list.getSelectedId() && current && !current.processed) ?
        ($$('processButton').enable(), $$('removeButton').enable()) :
        ($$('processButton').disable(), $$('removeButton').disable());
};

appP._onUpdateCallback = function (sender, url)
{
	this._send(url, null, webix.bind(this._onUpdateRequestSuccess, this));
};

appP._onUpdateRequestSuccess = function (text, data)
{
	var events = this._parseEvents(data.json());
	if (events)
	{
		if (typeof (events) == "string")
			webix.message(events, "error")
		else
			this._appendNewEvents(events);
	}
};

appP._sendProcessRequest = function (event)
{
	this._send(
		Eyelock.Service.ProcessEvent,
		JSON.stringify({ 'obj': JSON.stringify(event) }),
		webix.bind(this._onProcessed, this));
};

appP._sendRemoveRequest = function (event)
{
    this._send(
		Eyelock.Service.RemoveEvent,
		JSON.stringify({ 'obj': JSON.stringify(event) }),
		webix.bind(this._onRemoved, this));
};

appP._send = function (url, params, callback)
{
	webix
        .ajax()
        .header({ "Content-Type": "application/json; charset=UTF-8" })
        .post(url, params,
        {
        	success: callback,
        	error: webix.assert_error
        });
};

appP._onProcessed = function (result, args)
{
    // пометить как успешно обработанное ???
    var list = $$("queue"),
        current = list.getCursor();

    if (current)
        list.data.getItem(current).processed = true;

    this.goToNextListItem();
};

appP._onRemoved = function (result, args)
{
    // пометить как успешно обработанное ???
    var list = $$("queue"),
        current = list.getCursor();

    if (current && current != -1)
        list.data.remove(current);

    this.goToNextListItem();
};

appP = null;