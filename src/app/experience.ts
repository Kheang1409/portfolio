export class Experience {
    id: string;
    role: string;
    company: string;
    description: string;

    constructor(id: string, role: string, company: string, description: string) {
        this.id = id;
        this.role = role;
        this.company = company;
        this.description = description;
    }
}
