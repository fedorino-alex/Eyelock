(function() {
	if (!Eyelock.Service)
	{
		Eyelock.Service = {
			GetNewEvents: "Service/QueueService.asmx/GetNewEvents",
			GetAllEvents: "Service/QueueService.asmx/GetAllEvents",
			ProcessEvent: "Service/QueueService.asmx/ProcessEvent",
			RemoveEvent: "Service/QueueService.asmx/RemoveEvent",
			Find: "Service/QueueService.asmx/Find",
		};
	}

	webix.i18n.parseFormatDate = function (date)
	{
	    if (!date)
	        return '';
	    if (typeof date == 'object')
	        return date;
	    var set = [0, 0, 1, 0, 0, 0];
	    var temp = date.split(/[^0-9a-zA-Z]+/g);
	    set[0] = temp[0] || 0;
	    set[1] = (temp[1] || 1) - 1;
	    set[2] = temp[2] || 1;

	    return new Date(set[2], set[1], set[0], set[3], set[4], set[5]);
	};
})();