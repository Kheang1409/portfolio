export class Project {
    #id!: string;
    #title!: string;
    #description!: string;

    constructor(id: string, title: string, description: string) {
        this.#id = id;
        this.#title = title;
        this.#description = description;
    }

    get id(): string {
        return this.#id
    }
    get title(): string {
        return this.#title
    }
    get description(): string {
        return this.#description;
    }

    set id(id: string) {
        this.#id = id;
    }
    set title(title: string) {
        this.#title = title;
    }
    set description(description: string) {
        this.#description = description;
    }
}