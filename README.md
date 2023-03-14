# ClassDeclarationTemplateCreator
Extension to utilize auto complete in visual studio to quickly create a class initialization template.

Below is an example in which I created a class called Test1, and by keying in a fullstop and invokes suggest menu, I am able to select the class template that I want.

https://user-images.githubusercontent.com/5206035/221620415-d798804d-2d83-41b5-81e9-dd6b10af06dc.mp4



There are still much to be done, such as;
1. Parse attribute value to generate template implicit (Test test = new Test() {}) vs explicit (Test test = new Test(); test.abc = "";).
2. Format the output, not sure if this is possible though.
3. Support initialization of more properties.
