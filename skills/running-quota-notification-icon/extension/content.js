(() => {
  const ROOT_ID = "clintware-quota-indicator";
  const DEFAULTS = {
    mode: "unavailable",
    dailyLimit: 50,
    usedToday: 0,
    dayKey: new Date().toLocaleDateString("en-CA"),
    notifications: true
  };

  let state = { ...DEFAULTS };
  let panelOpen = false;
  let lastSubmitAt = 0;

  const storage = chrome?.storage?.local;

  function todayKey() {
    return new Date().toLocaleDateString("en-CA");
  }

  function resetIfNeeded(value) {
    if (value.dayKey !== todayKey()) {
      return { ...value, usedToday: 0, dayKey: todayKey() };
    }
    return value;
  }

  function percentRemaining() {
    if (state.mode !== "estimated" || !Number.isFinite(state.dailyLimit) || state.dailyLimit <= 0) return null;
    return Math.max(0, Math.min(100, Math.round(((state.dailyLimit - state.usedToday) / state.dailyLimit) * 100)));
  }

  function quotaState(percent) {
    if (percent === null) return "unavailable";
    if (percent === 0) return "exhausted";
    if (percent <= 5) return "critical";
    if (percent <= 20) return "low";
    if (percent <= 50) return "moderate";
    return "healthy";
  }

  function label(percent) {
    if (percent === null) return "Quota unavailable";
    return `Estimated: ${percent}%`;
  }

  function save() {
    storage?.set(state);
  }

  function notifyThreshold(previous, current) {
    if (!state.notifications || previous === null || current === null) return;
    const thresholds = [20, 5, 0];
    for (const threshold of thresholds) {
      if (previous > threshold && current <= threshold) {
        chrome.runtime?.sendMessage?.({ type: "quota-threshold", threshold, percent: current });
        break;
      }
    }
  }

  function render() {
    document.getElementById(ROOT_ID)?.remove();
    const percent = percentRemaining();
    const level = quotaState(percent);
    const root = document.createElement("div");
    root.id = ROOT_ID;
    root.dataset.state = level;

    const button = document.createElement("button");
    button.className = "cwq-button";
    button.type = "button";
    button.setAttribute("aria-expanded", String(panelOpen));
    button.setAttribute("aria-label", percent === null ? "Work quota unavailable" : `Work quota: estimated ${percent} percent remaining`);
    button.innerHTML = `<span class="cwq-gauge" aria-hidden="true"></span><span class="cwq-label">${label(percent)}</span>`;
    button.addEventListener("click", () => {
      panelOpen = !panelOpen;
      render();
    });
    root.appendChild(button);

    if (panelOpen) {
      const panel = document.createElement("section");
      panel.className = "cwq-panel";
      panel.setAttribute("aria-label", "Work quota details");
      const remaining = Math.max(0, state.dailyLimit - state.usedToday);
      panel.innerHTML = percent === null
        ? `<h2>Work quota</h2><p><strong>Quota unavailable</strong></p><p>ChatGPT does not expose a trustworthy live quota value to this extension.</p><p>Enable local estimate mode in settings to track submitted prompts against a limit you choose.</p>`
        : `<h2>Work quota</h2><p><strong>${percent}% remaining</strong></p><div class="cwq-progress"><span style="width:${percent}%"></span></div><dl><div><dt>Remaining</dt><dd>${remaining} of ${state.dailyLimit} tasks</dd></div><div><dt>Used today</dt><dd>${state.usedToday}</dd></div><div><dt>Accuracy</dt><dd>Estimated</dd></div><div><dt>Source</dt><dd>Locally counted submitted prompts</dd></div><div><dt>Reset</dt><dd>Local midnight</dd></div></dl>`;

      const controls = document.createElement("div");
      controls.className = "cwq-controls";
      const settings = document.createElement("button");
      settings.type = "button";
      settings.textContent = "Settings";
      settings.addEventListener("click", () => chrome.runtime.sendMessage({ type: "open-options" }));
      controls.appendChild(settings);
      if (percent !== null) {
        const undo = document.createElement("button");
        undo.type = "button";
        undo.textContent = "Undo one";
        undo.disabled = state.usedToday <= 0;
        undo.addEventListener("click", () => {
          state.usedToday = Math.max(0, state.usedToday - 1);
          save();
          render();
        });
        controls.appendChild(undo);
      }
      panel.appendChild(controls);
      root.appendChild(panel);
    }

    document.body.appendChild(root);
  }

  function countSubmission() {
    if (state.mode !== "estimated") return;
    const now = Date.now();
    if (now - lastSubmitAt < 1500) return;
    lastSubmitAt = now;
    const previous = percentRemaining();
    state.usedToday += 1;
    state.dayKey = todayKey();
    const current = percentRemaining();
    save();
    notifyThreshold(previous, current);
    render();
  }

  document.addEventListener("click", (event) => {
    const target = event.target instanceof Element ? event.target.closest("button") : null;
    if (!target) return;
    const aria = (target.getAttribute("aria-label") || "").toLowerCase();
    const dataTestId = (target.getAttribute("data-testid") || "").toLowerCase();
    if (aria.includes("send") || dataTestId.includes("send-button")) countSubmission();
  }, true);

  document.addEventListener("keydown", (event) => {
    if (event.key !== "Enter" || event.shiftKey) return;
    const target = event.target;
    if (!(target instanceof HTMLElement)) return;
    if (target.matches("textarea, [contenteditable='true']")) countSubmission();
  }, true);

  storage?.get(DEFAULTS, (value) => {
    state = resetIfNeeded({ ...DEFAULTS, ...value });
    save();
    render();
  });

  chrome.storage?.onChanged?.addListener((changes, area) => {
    if (area !== "local") return;
    for (const [key, change] of Object.entries(changes)) state[key] = change.newValue;
    state = resetIfNeeded(state);
    render();
  });
})();
