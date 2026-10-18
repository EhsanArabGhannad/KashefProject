(() => {
  const upload = document.querySelector('input[name="Image"]');
  const preview = document.querySelector('#calendar-image-preview');
  if (!upload || !preview) return;
  const original = preview.getAttribute('src');
  const remove = document.querySelector('input[name="RemoveImage"]');
  let temporary;
  function update() {
    if (temporary) URL.revokeObjectURL(temporary);
    temporary = null;
    const file = upload.files[0];
    const valid = file && ['image/jpeg', 'image/png', 'image/webp'].includes(file.type) && file.size <= 8 * 1024 * 1024;
    temporary = valid ? URL.createObjectURL(file) : null;
    const source = temporary || (!remove?.checked ? original : null);
    preview.hidden = !source;
    if (source) preview.src = source;
    else preview.removeAttribute('src');
  }
  upload.addEventListener('change', update);
  remove?.addEventListener('change', update);
  document.querySelector('input[name="TitleEn"]')?.addEventListener('input', event => {
    document.querySelector('#calendar-preview-title').textContent = event.target.value;
  });
  document.querySelector('input[name="Day"]')?.addEventListener('input', event => {
    document.querySelector('#calendar-preview-number').textContent = event.target.value;
  });
  window.addEventListener('pagehide', () => { if (temporary) URL.revokeObjectURL(temporary); });
  window.addEventListener('pageshow', event => { if (event.persisted) update(); });
})();
