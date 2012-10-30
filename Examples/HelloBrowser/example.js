function Find(name, lookup) {
    return name == "WA" ? { name: "Seattle", population: lookup("Seattle") } : null;
}

function show(person) {
    var div = document.createElement("DIV");
    person.render(div);
    document.body.appendChild(div);
}
