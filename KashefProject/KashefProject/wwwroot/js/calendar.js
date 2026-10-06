(() => {
  if (!document.querySelector('.heritage-calendar') || !window.fetch || !window.AbortController) return;
  let pending;
  const position = () => ({ x: window.scrollX, y: window.scrollY });
  const restore = point => window.scrollTo({ left: point.x, top: point.y, behavior: 'instant' });
  const remember = point => history.replaceState({ ...history.state, calendarScroll: point }, '', location.href);
  history.scrollRestoration = 'manual';
  remember(position());

  async function navigate(url, trigger, fromHistory = false, historyPoint = null) {
    pending?.abort();
    const request = new AbortController();
    pending = request;
    const current = document.querySelector('.heritage-calendar');
    current.setAttribute('aria-busy', 'true');
    const status = current.querySelector('.calendar-status');
    status.classList.remove('is-error');
    status.textContent = '';
    // Remember focus by control identity, never scroll the details panel into view.
    const day = trigger?.dataset.calendarDay;
    const isSubmit = trigger?.matches('button[type="submit"]');
    const href = trigger?.getAttribute('href');
    const monthDirection = trigger?.dataset.calendarDirection;
    try {
      const response = await fetch(url, { credentials: 'same-origin', signal: request.signal });
      if (!response.ok) throw new Error('Calendar request failed');
      const page = new DOMParser().parseFromString(await response.text(), 'text/html');
      const next = page.querySelector('.heritage-calendar');
      if (!next || request.signal.aborted) throw new Error('Invalid calendar response');
      const point = historyPoint || position();
      if (!fromHistory) {
        remember(point);
        history.pushState({ calendarScroll: point }, '', url);
      }
      current.replaceWith(next);
      document.title = page.title;
      document.documentElement.lang = next.lang;
      const focus = day ? next.querySelector(`[data-calendar-day="${day}"]`)
        : isSubmit ? next.querySelector('.calendar-controls button')
        : monthDirection ? next.querySelector(`[data-calendar-direction="${monthDirection}"]`)
        : [...next.querySelectorAll('[data-calendar-nav]')].find(link => link.getAttribute('href') === href);
      focus?.focus({ preventScroll: true });
      restore(point);
      requestAnimationFrame(() => {
        if (pending !== request) return;
        restore(point);
        next.querySelector('.calendar-status').textContent = next.dataset.updateMessage + ' ' + next.querySelector('#selected-day-title').textContent;
      });
    } catch (error) {
      if (error.name === 'AbortError' || request.signal.aborted) return;
      status.textContent = current.dataset.errorMessage;
      status.classList.add('is-error');
    } finally {
      current.removeAttribute('aria-busy');
    }
  }

  document.addEventListener('click', event => {
    const link = event.target.closest('.heritage-calendar a[data-calendar-nav]');
    if (!link || event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
    const url = new URL(link.href, location.href);
    if (url.origin !== location.origin || url.pathname.replace(/\/$/, '') !== '/calendar') return;
    event.preventDefault();
    navigate(url.href, link);
  });
  document.addEventListener('submit', event => {
    if (!event.target.matches('.heritage-calendar .calendar-controls') || event.defaultPrevented) return;
    event.preventDefault();
    const url = new URL(event.target.action, location.href);
    url.search = new URLSearchParams(new FormData(event.target)).toString();
    navigate(url.href, event.submitter);
  });
  // Store scroll on the current history entry so Back/Forward also stay in context.
  let scrollTimer;
  window.addEventListener('scroll', () => {
    clearTimeout(scrollTimer);
    scrollTimer = setTimeout(() => remember(position()), 500);
  }, { passive: true });
  window.addEventListener('pagehide', () => remember(position()));
  window.addEventListener('popstate', event => {
    if (location.pathname.replace(/\/$/, '') === '/calendar') navigate(location.href, null, true, event.state?.calendarScroll);
  });
})();
