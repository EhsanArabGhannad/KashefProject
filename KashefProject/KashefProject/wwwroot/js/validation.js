// ASP.NET emits boolean consent ranges as "True"; numeric range validation
// rejects even a checked box. Map these consent rules to a required checkbox.
$.validator.unobtrusive.adapters.add("range", ["min", "max"], (options) => {
  if (options.element.type !== "checkbox" ||
      options.params.min.toLowerCase() !== "true" || options.params.max.toLowerCase() !== "true") return;
  delete options.rules.range;
  options.rules.required = true;
  options.messages.required = options.message;
});
