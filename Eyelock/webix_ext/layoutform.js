webix.protoUI({
	name: "layoutform",
	$init: function(config)
	{
		
	},
	render: function() {},
	defaults: 
	{
		type: "layoutform"
	},
	_parse_cells: function()
	{
		webix.ui.layout.prototype._parse_cells.call(this);
		this._forms = this._getForms(this._forms);
	},
	_getForms: function (forms, component)
	{
		forms = forms || [];
		component = component || this;
		var collection = component.getChildViews();

		for (var i = 0; i < collection.length; i++)
		{
			item = collection[i];
			if (item.name == 'form')
				forms.push(item);
			else
				this._getForms(forms, item);
		}

		return forms;
	},
	_eachForm: function(functor, arguments, aggregate)
	{
		if (this._forms)
		{
			var result;
			for (var i = 0; i < this._forms.length; i++)
			{
				result = functor.apply(this._forms[i], arguments)
				if (aggregate)
					result = aggregate(result);
			}

			return result;
		}
	},

	// region webix.Values
	setValues: function ()
	{
		this._eachForm(webix.Values.setValues, arguments);
	},
	isDirty: function ()
	{
		var result = false;
		this._eachForm(webix.Values.isDirty, arguments, function (res) { return result || res; });
		return result;
	},
	getDirtyValues: function ()
	{
		var result = {};
		return this._eachForm(webix.Values.getDirtyValues, arguments, function (res) { return webix.extend(result, res); });
	},
	getValues: function ()
	{
		var result = {};
		return this._eachForm(webix.Values.getValues, arguments, function (res) { return webix.extend(result, res); });
	},
	clear: function ()
	{
		this.setValues({ });
	},
	// ---------------------------------

	// region webix.ValidateData

	clearValidation: function ()
	{
		this._eachForm(webix.ValidateData.clearValidation, arguments);
	},

	validate: function ()
	{
		var result = true;
		return this._eachForm(webix.ValidateData.validate, arguments, function (res) { return result && res; });
	}

	// ---------------------------------

},  webix.AtomDataLoader, webix.Values, webix.ui.layout, webix.ValidateData);
	

webix.protoUI({
	name: "image",
	$init: function (config) { },
	_allowsClear: true,
	_no_image_url: 'img/no_eye_image.png',
	defaults:
	{
		template: "<img src='img/no_eye_image.png'></img>",
		autoheight: true
	},
	$setValue: function(value)
	{
		value = value || this._no_image_url;
		this._dataobj.firstChild.setAttribute("src", value);
	}

}, webix.ui.label)