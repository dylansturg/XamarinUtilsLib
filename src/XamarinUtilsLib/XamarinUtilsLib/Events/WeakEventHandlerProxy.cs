using System;
using System.Reflection;

namespace DylanSturg.XamarinUtilsLib
{
	public class WeakEventHandlerProxy<TArgs> where TArgs : EventArgs
	{
		public object Target
		{
			get
			{
				object strongTarget;
				if (weakTarget.TryGetTarget(out strongTarget))
				{
					return strongTarget;
				}
				return null;
			}
		}

		readonly WeakReference<object> weakTarget;
		readonly MethodInfo eventAction;

		public WeakEventHandlerProxy(EventHandler eventHandler)
		{
			weakTarget = new WeakReference<object>(eventHandler.Target);
			eventAction = eventHandler.GetMethodInfo();
		}

		public void RaiseEvent(object sender, TArgs args)
		{
			object strongTarget = null;
			weakTarget.TryGetTarget(out strongTarget);

			var actionParams = eventAction.GetParameters();
			var actionArgs = new object[actionParams.Length];

			if (actionParams.Length >= 1)
			{
				actionArgs[0] = sender;
			}

			if (actionParams.Length >= 2)
			{
				actionArgs[1] = args;
			}

			eventAction.Invoke(strongTarget, actionArgs);
		}

	}
}

