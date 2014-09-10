if (!window.Eyelock)
	window.Eyelock = {};

Eyelock.Search = function (settings)
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
};

var appP = Eyelock.Search.prototype;

appP.init = function ()
{
	this._initLayout();
	this._bind();
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
					}
				]
			},
			{
				cols: [
					{
						rows: [
						{
							type: 'line',
							borderless: true,
							rows:
								[
									{
										id: 'fname',
										view: "text",
										name: "firstName",
										placeholder: "Имя",
										keyPressTimeout: 500
									},
									{
										id: 'lname',
										view: "text",
										name: "lastName",
										placeholder: "Фамилия",
										keyPressTimeout: 500
									},
								]
							},
						{
							view: "list",
							id: "queue",
							pager: 'queuePager',
							minWidth: 320,
							width: 320,
							data: [],
							template: "<div><b>#firstName# #lastName#</b><div>#dob#</div></div>",
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
							}
						},
						{
							view: 'pager',
							id: 'queuePager',
							size: 50,
							group: 4,
							template: '{common.first()} {common.prev()} {common.pages()} {common.next()} {common.last()}',
						}]
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
								this._getPreviewForm()
							]
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
					{ view: 'text', label: 'Имя:', name: 'firstName', labelWidth: 180, readonly: true },
					{ view: 'text', label: 'Фамилия:', name: 'lastName', labelWidth: 180, readonly: true },
					{ view: 'text', label: 'Дата рождения:', name: 'dob', labelWidth: 180, format: "%d-%m-%Y", readonly: true },
				]
			},
			{}
		]
    };

	return form;
};

appP._getPreviewForm = function ()
{
	return this._getForm('Просмотр профиля', 'view');
};

appP._bind = function ()
{
	// набор связываний и обработчиков событий
	var list = $$("queue");
	$$("view").bind(list);

	var handler = webix.bind(this._onSearchChanged, this);
	$$('fname').attachEvent('onTimedKeyPress', handler);
	$$('lname').attachEvent('onTimedKeyPress', handler);
};

appP._onSearchChanged = function (sender, args)
{
	var fnValue = ($$('fname').getValue() || "").trim(),
		lnValue = ($$('lname').getValue() || "").trim();

	if (fnValue || lnValue)
		this._send(Eyelock.Service.Find, { first: fnValue, last: lnValue }, webix.bind(this._populateList, this));
	else
		this._populateList(); // очищает список
};

appP._send = function (url, params, callback)
{
	webix
        .ajax()
        .header({ "Content-Type": "application/json" })
        .post(url, JSON.stringify(params),
        {
        	success: callback,
        	error: webix.assert_error
        });
};

appP._parseUsers = function (users)
{
	var user = null, result = [];
	users = users.d || users;
	if (users.IsSuccess)
	{
		users = users.Result;
		for (var i = 0; i < users.length; i++)
		{
			user = users[i];

			delete user["__type"];
			delete user["ExtensionData"];
			delete user["uid"];

			user.firstName = user.firstName || "";
			user.lastName = user.lastName || "";
			user.dob = user.dob || "";

			result.push(user);
		}
	}
	else
	{
		result = users.ErrorMessage;
	}

	return result;
};

appP._populateList = function (text, data)
{
	var listData = $$('queue').data;
	listData.clearAll();
	$$("view").setValues({});

	if (data)
	{
		var users = this._parseUsers(data.json());
		for (var i = 0; i < users.length; i++)
			listData.add(users[i])
	}

	listData.refresh();
};

appP = null;