chrome.runtime.onMessage.addListener((message) => {
  if (message?.type === "open-options") {
    chrome.runtime.openOptionsPage();
    return;
  }

  if (message?.type === "quota-threshold") {
    chrome.notifications.create({
      type: "basic",
      iconUrl: "icon.svg",
      title: "Work quota update",
      message: `Estimated quota: ${message.percent}% remaining`
    });
  }
});

chrome.action.onClicked.addListener(() => chrome.runtime.openOptionsPage());
