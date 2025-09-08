import os, json, sys

if __name__ == "__main__":
    #json_str = json.dumps(result['Children'], indent=2)
    all_node = []
    all_documents = []
    with open("rasults.json", "r", encoding="utf-8") as out_stream:
        all_node = json.load(out_stream)
    for x in all_node:
        second_step = x['Children']
        for y in second_step:
            third_step = y['Children']#list of document
            for counter in range(len(third_step)):
                document = third_step[counter]['Name']
                document_part = str(document).split('-')
                document_name = (document_part[6:])[0]
                document_number = str(document).replace(document_name,'')
                if document_number.endswith('-'):
                    document_number = document_number[:len(document_number)-1]
                children = third_step[counter]['Children']
                revisions = [item['Name'] for item in children]
                all_documents.append({
                    "Document_name" : document_name,
                    "Document_number" : document_number ,
                    "Dicipline" : x['Name'],
                    "Document_type" : y['Name'],
                    "Revisions" : revisions,
                })
    breakpoint()
    json_str = json.dumps(all_documents, indent=2)
    with open("database.json", "w", encoding="utf-8") as out_stream:
        out_stream.write(json_str)
    
