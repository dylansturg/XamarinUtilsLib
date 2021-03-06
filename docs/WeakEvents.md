# Weak Event Handlers

[WeakEventHandlerProxy](../src/XamarinUtilsLib/XamarinUtilsLib/Events/WeakEventHandlerProxy.cs)

### Prevent Memory Leaks by *weakly* Subscribing to Events

#### Intro to Weak Event Subscription
C# events always maintain strong references to the EventHandler instances which have been registered for the event (usually with the += operator). Each EventHandler in turn maintains a strong reference to the target of the invocation.

When the event signaler (the object which raises the event) outlives the signaler and unsubscribing from the event is unfeasible, then the subscribing object has to stay alive as long as the signaler does.

By using a proxy object to forward the event from the signaler to the subscriber, it is possible to break the strong relationship between the two. This approach allows the subscriber to be garbage collected before the signaler without the need to unsubscribe from the event.

#### How Does This Magic Work?
Instead of directly subscribing to an event the subscriber uses a lightweight proxy object to forward the event back to itself. WeakEventHandlerProxy provides this functionality by storing the MethodInfo of an EventHandler and a weak reference to its expected Target object. The weak reference ensures that the subscriber can be garbage collected when it is no longer needed without waiting for the event signaler. 

```C#
public WeakEventHandlerProxy(EventHandler eventHandler)
{
	weakTarget = new WeakReference<object>(eventHandler.object);
	// Target represents the object which the encapsulated method is invoked on
	eventAction = eventHandler.GetMethodInfo();
	// EventHandler maintains a strong reference to the Target
	// MethodInfo does not so it is safe to store
}
```
Once the event is raised, the proxy will use `eventAction` with `weakTarget` (after getting a strong reference to the Target, if available) can be used to call the original method.

If the original subscriber has been garbage collected, then the proxy simply does not forward the event.

##### Example Usage

```C#
TypeWithAnEvent signaler;

public void Subscribe()
{
	var proxy = new WeakEventHandlerProxy<EventArgs>(new EventHandler(HandleEvent));
	signaler.EventHandler += proxy.RaiseEvent;
}

private void HandleEvent(object sender, EventArgs args)
{
	// Handle the event you were expecting
}
```

#### Why Do I Need This?
I've found this technique is helpful when using events in Xamarin.iOS. Objects that live in both Mono's managed environment and the Objective-C (or Swift) runtime have complex garbage collection semantics. Reference cycles which could normally be collected by the garbage collector will lead to unbreakable memory leaks. These cycles occur mostly in the UI layer, because each object will be rooted in the Objective-C runtime and Mono can't collect them.

The best approach is to always remove your event subscriptions when they are no longer necessary. This is pretty easy when you're working in a `UIViewController` subclass, because there is extensive lifecycle support, like in the example below.

```C#
public override void ViewWillAppear(bool animated)
{
	// assume this UIViewController has a UIButton named button
	button.TouchUpInside += HandleButtonTap;
}

public override void ViewWillDisappear(bool animated)
{
	button.TouchUpInside -= HandleButtonTap;
}

private void HandleButtonTap(object sender, EventArgs)
{
	// React to the button
}
```

If the subscriber does not have such extensive lifecycle support, like a `UIView` subclass, then removing event subscriptions gets more difficult. It's possible to setup your own support for lifecycle events, but that adds a lot of code and is easy to get wrong.

Imagine a custom `UIView` defined in a xib with a `UIButton`.

```C#
public override void AwakeFromNib()
{
	button.TouchUpInside += HandleButtonTap;
	// When do you unsubscribe?
	// Might memory leak without removing the handler
	// button.TouchUpInside -= HandleButtonTap;
	
	// Or use a weak proxy, and you don't need to unsubscribe
	var proxy = new WeakEventHandlerProxy<EventArgs>(new EventHandler(HandleButtonTap));
	button.TouchUpInside += proxy.RaiseEvent;
}

private void HandleButtonTap(object sender, EventArgs args)
{
	// ...
}
```

##### What Am I Losing?
Unfortunately, I have not found a silver bullet. The `WeakEventHandlerProxy` allows us to forget about some garbage collection concerns, but is slower than just subscribing to an event. It uses reflection to invoke the given delegate, which is inherently slower than directly invoking the delegate. 

I've found that the performance hit is usually minimal, with less a millisecond difference per event firing. I would be cautious of the performance implications for events that fire a lot (probably more than 100 times / second, but I have no real data and it would depend heavily on the application).

##### Long Lived Signaler
In the event that the signaler is expected to outlive the subscriber, this approach has a considerable disadvantage. If the event subscription is not removed, then the `WeakEventHandlerProxy` will stay alive as long as the signaler object. Since these are relatively lightweight, it might not be a concern for a given application. If it is, it would be possible to remove the event handler in a finalizer or dispose method.
