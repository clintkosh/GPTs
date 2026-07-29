const DEFAULTS = {
  mode: "unavailable",
  dailyLimit: 50,
  notifications: true
};

const form = document.querySelector("#settings-form");
const mode = document.querySelector("#mode");
const dailyLimit = document.querySelector("#daily-limit");
const notifications = document.querySelector("#notifications");
const status = document.querySelector("#status");

chrome.storage.local.get(DEFAULTS, (value) => {
  mode.value = value.mode;
  dailyLimit.value = value.dailyLimit;
  notifications.checked = value.notifications;
});

form.addEventListener("submit", (event) => {
  event.preventDefault();
  const limit = Number.parseInt(dailyLimit.value, 10);
  if (!Number.isFinite(limit) || limit < 1) {
    status.textContent = "Enter a daily limit of at least 1.";
    return;
  }

  chrome.storage.local.set({
    mode: mode.value,
    dailyLimit: limit,
    notifications: notifications.checked,
    usedToday: 0,
    dayKey: new Date().toLocaleDateString("en-CA")
  }, () => {
    status.textContent = "Settings saved. Today's local estimate was reset.";
  });
});
