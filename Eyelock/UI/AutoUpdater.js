if (!window.Eyelock)
    window.Eyelock = {};

Eyelock.AutoUpdater = function (settings)
{
    this._delay = settings.AutoUpdateInterval || 1000;
    this._callback = settings.UpdateCallback;
    this._intervalId = -1;
    this._url = settings.GetNewEvents;
};

var updP = Eyelock.AutoUpdater.prototype;

updP.getIsEnabled = function ()
{
    return (this._intervalId != -1);
};

updP.Start = function ()
{
    if (this._intervalId != -1)
        return true;

    if (this._delay > 0 && this._callback)
        return !!(this._intervalId = setInterval(webix.bind(this._onTimer, this), this._delay, { context: this }));

    return false;
};

updP.Stop = function ()
{
    if (this._intervalId != -1)
    {
        clearInterval(this._intervalId);
        this._intervalId = -1;
    }
};

updP.setDelay = function (delay)
{
    if (delay != this._delay)
    {
        this._delay = delay;
        this.Refresh();
    }
};

updP.Refresh = function ()
{
    this.Stop();
    this.Start();
};

updP._onTimer = function (args)
{
    if (this._callback)
        this._callback(this, this._url);
};

updP = null;