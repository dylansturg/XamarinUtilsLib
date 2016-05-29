# Weak Event Handlers
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

