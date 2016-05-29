using System;
using NUnit.Framework;
using DylanSturg.XamarinUtilsLib;

namespace DylanSturg.XamarinUtilsLib.Tests
{
	[TestFixture]
	public class WeakEventHandlerProxyTests
	{
		[Test]
		public void WeakEventHandlerProxy_Invokes_EventHandler_With_Null_Target()
		{
			var testHandler = new EventHandler((sender, e) => { });
			Assert.IsNull(testHandler.Target, $"{nameof(EventHandler)} under test should have a null {nameof(EventHandler.Target)}");
			var testProxy = new WeakEventHandlerProxy<EventArgs>(testHandler);
			Assert.IsNull(testProxy.Target, $"{nameof(WeakEventHandlerProxy<EventArgs>)} should store null Target");
			Assert.DoesNotThrow(() =>
								testProxy.RaiseEvent(new object(), new EventArgs()));
		}

		[Test]
		public void WeakEventHandlerProxy_RaiseEvent_Invokes_Given_EventHandler()
		{
			var handlerInvoked = false;
			var testHandler = new EventHandler((sender, e) =>
			{
				handlerInvoked = true;
			});

			var testProxy = new WeakEventHandlerProxy<EventArgs>(testHandler);
			Assert.AreEqual(testHandler.Target, testProxy.Target);
			testProxy.RaiseEvent(new object(), new EventArgs());

			Assert.IsTrue(handlerInvoked, $"{nameof(WeakEventHandlerProxy<EventArgs>.RaiseEvent)} should invoke EventHandler from constructor");
		}

		[Test]
		public void WeakEventHandlerProxy_RaiseEvent_Invokes_With_Given_Sender_And_EventArgs()
		{
			var expectedSender = new object();
			var expectedArgs = new EventArgs();

			var handlerInvoked = false;
			var testHandler = new EventHandler((sender, args) =>
			{
				handlerInvoked = true;
				Assert.AreEqual(expectedArgs, args);
				Assert.AreEqual(expectedSender, sender);
			});

			var testProxy = new WeakEventHandlerProxy<EventArgs>(testHandler);
			testProxy.RaiseEvent(expectedSender, expectedArgs);

			Assert.IsTrue(handlerInvoked, $"{nameof(WeakEventHandlerProxy<EventArgs>.RaiseEvent)} should invoke EventHandler with given sender and EventArgs");
		}
	}
}

