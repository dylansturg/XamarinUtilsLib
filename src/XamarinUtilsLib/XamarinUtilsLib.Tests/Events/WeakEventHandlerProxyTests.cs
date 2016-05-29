using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

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

		[Test]
		public void WeakEventHandler_Handles_Generic_EventHandler()
		{
			var testHandler = new EventHandler<CustomEventArgs<object>>(CustomEventArgsHandler);
			Assert.AreEqual(this, testHandler.Target);

			var testProxy = new WeakEventHandlerProxy<CustomEventArgs<object>>(testHandler);
			Assert.AreEqual(this, testProxy.Target);
		}

		private void CustomEventArgsHandler(object sender, CustomEventArgs<object> args)
		{
		}

		[Test]
		public void WeakEventHandler_RaiseEvent_Invokes_With_Generic_EventArgs()
		{
			var expectedSender = new object();
			var expectedArgs = new CustomEventArgs<object>
			{
				EventData = new object()
			};

			var handlerCalled = false;
			var testProxy = new WeakEventHandlerProxy<CustomEventArgs<object>>((Action<object, CustomEventArgs<object>>)((sender, e) =>
			{
				handlerCalled = true;
				Assert.AreEqual(expectedSender, sender);
				Assert.AreEqual(expectedArgs, e);
			}));

			testProxy.RaiseEvent(expectedSender, expectedArgs);

			Assert.IsTrue(handlerCalled);
		}

		[Test]
		public async Task WeakEventHandler_Maintains_Weak_Reference_For_Target()
		{
			WeakEventHandlerProxy<EventArgs> proxy;

			{
				var subscriber = new CustomEventSubscriber<object>();
				proxy = new WeakEventHandlerProxy<EventArgs>((Action<object, EventArgs>)subscriber.HandleEvent);
				Assert.AreEqual(subscriber, proxy.Target, "Proxy Target should be the event subscriber");
				subscriber = null;
			}

			var waitCount = 0;
			var maximumWaitCount = 10;

			while (waitCount < maximumWaitCount)
			{
				waitCount++;

				if (proxy.Target == null)
				{
					break;
				}

				await Task.Delay(10);

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			}

			Assert.IsNull(proxy.Target, "Proxy Target should be garbage collected, proving it is a weak reference.");
		}

		[Test]
		public void WeakEventHandlerProxy_E2E_Signaler_Invokes_Subscriber()
		{
			var signaler = new CustomEventSignaler();

			var expectedObjectArgs = new CustomEventArgs<object>();

			var mockObjectSubscriber = new Mock<CustomEventSubscriber<CustomEventArgs<object>>>();
			mockObjectSubscriber.Setup(mock => mock.HandleEvent(signaler, expectedObjectArgs))
								.Verifiable($"{nameof(WeakEventHandlerProxy<CustomEventArgs<object>>)} should invoke subscriber when event is raised");

			var objectProxy = new WeakEventHandlerProxy<CustomEventArgs<object>>(new EventHandler<CustomEventArgs<object>>(mockObjectSubscriber.Object.HandleEvent));
			signaler.ObjectChanged += objectProxy.RaiseEvent;
			signaler.RaiseObjectChanged(expectedObjectArgs);
			mockObjectSubscriber.Verify();

			var expectedPropertyArgs = new PropertyChangedEventArgs("Property");
			var mockPropertySubscriber = new Mock<CustomEventSubscriber<PropertyChangedEventArgs>>();
			mockPropertySubscriber.Setup(mock => mock.HandleEvent(signaler, expectedPropertyArgs))
								  .Verifiable($"{nameof(WeakEventHandlerProxy<PropertyChangedEventArgs>)} should invoke subscriber when event is raised");
			var propertyProxy = new WeakEventHandlerProxy<PropertyChangedEventArgs>(new EventHandler<PropertyChangedEventArgs>(mockPropertySubscriber.Object.HandleEvent));
			signaler.PropertyChanged += propertyProxy.RaiseEvent;
			signaler.RaisePropertyChanged(expectedPropertyArgs);
			mockPropertySubscriber.Verify();
		}
	}

	public class CustomEventArgs<T> : EventArgs
	{
		public T EventData { get; set; }
	}

	public class CustomEventSubscriber<T>
	{
		public virtual void HandleEvent(object sender, T args)
		{
		}
	}

	public class CustomEventSignaler
	{
		public delegate void ObjectEventHandler(object sender, CustomEventArgs<object> args);

		public event PropertyChangedEventHandler PropertyChanged;
		public event ObjectEventHandler ObjectChanged;

		public void RaisePropertyChanged(PropertyChangedEventArgs args)
		{
			PropertyChanged?.Invoke(this, args);
		}

		public void RaiseObjectChanged(CustomEventArgs<object> args)
		{
			ObjectChanged?.Invoke(this, args);
		}
	}
}

