export class Education {
    id: string;
    degree: string;
    university: string;
    description: string;

    constructor(id: string, degree: string, university: string, description: string) {
        this.id = id;
        this.degree = degree;
        this.university = university;
        this.description = description;
    }
}
