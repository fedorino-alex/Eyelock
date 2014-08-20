if (!window.Eyelock)
    window.Eyelock = {};

Eyelock.AutoUpdater = function (settings)
{
    this._delay = settings.AutoUpdateInterval || 5000;
    this._callback = settings.UpdateCallback;
    this._intervalId = -1;
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
        return !!(this._intervalId = setInterval(this._onTimer, this._delay, { context: this }));

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
    var context = args.context;
    if (context._callback)
        context._callback.call(this, context, { timestamp: Date.now() });
};

updP = null;